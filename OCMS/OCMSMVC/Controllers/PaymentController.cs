using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using OCMSMVC.Models;
using Services;

namespace OCMSMVC.Controllers;

public class PaymentController : Controller
{
    private readonly IClinicWorkflowService _workflow;
    private readonly IVnPayGatewayService _gateway;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentController> _logger;
    private readonly IEticketEmailSender _ticketEmailSender;

    public PaymentController(IClinicWorkflowService workflow, IVnPayGatewayService gateway, IConfiguration configuration, ILogger<PaymentController> logger, IEticketEmailSender ticketEmailSender)
    {
        _workflow = workflow;
        _gateway = gateway;
        _configuration = configuration;
        _logger = logger;
        _ticketEmailSender = ticketEmailSender;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deposit(int appointmentId, CancellationToken cancellationToken)
    {
        if (HttpContext.Session.GetString("Role") != "Patient") return RedirectToAction("Login", "Auth");
        var patientUserId = HttpContext.Session.GetInt32("UserId");
        if (patientUserId == null) return RedirectToAction("Login", "Auth");

        try
        {
            var payment = await _workflow.CreatePendingDepositAsync(appointmentId, patientUserId.Value, cancellationToken);
            var publicBaseUrl = _configuration["Vnpay:PublicBaseUrl"]?.TrimEnd('/');
            var returnUrl = !string.IsNullOrWhiteSpace(publicBaseUrl)
                ? $"{publicBaseUrl}/Payment/Return"
                : Url.Action(nameof(Return), "Payment", values: null, protocol: Request.Scheme)
                  ?? throw new InvalidOperationException("Không tạo được URL trả kết quả VNPAY.");
            var ipnUrl = _configuration["Vnpay:IpnUrl"];
            if (!Uri.TryCreate(ipnUrl, UriKind.Absolute, out var parsedIpnUrl) || parsedIpnUrl.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException("Vnpay:IpnUrl phải là URL HTTPS công khai trỏ đến /Payment/Ipn.");
            }

            var paymentUrl = _gateway.CreatePaymentUrl(payment, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1", returnUrl);
            return Redirect(paymentUrl);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpGet]
    public async Task<IActionResult> Return(CancellationToken cancellationToken)
    {
        var valid = _gateway.IsValidSignature(ReadVnpayParameters(), Request.Query["vnp_SecureHash"].ToString());
        var userId = HttpContext.Session.GetInt32("UserId");
        var paymentId = int.TryParse(Request.Query["vnp_TxnRef"], NumberStyles.None, CultureInfo.InvariantCulture, out var parsedId) ? parsedId : 0;
        var payment = valid && userId.HasValue && paymentId > 0
            ? await _workflow.GetPaymentStatusAsync(paymentId, userId.Value, cancellationToken)
            : null;

        var model = new VnPayReturnViewModel
        {
            SignatureValid = valid,
            Payment = payment,
            Message = !valid ? "Không xác thực được phản hồi của cổng thanh toán." :
                payment == null ? "Không tìm thấy giao dịch của tài khoản này." :
                payment.PaymentStatus == "Pending" ? "Đang chờ hệ thống xác nhận giao dịch từ VNPAY." : null
        };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Ipn(CancellationToken cancellationToken)
    {
        var parameters = ReadVnpayParameters();
        var hash = Request.Query["vnp_SecureHash"].ToString();
        if (!_gateway.IsValidSignature(parameters, hash))
        {
            return Json(new { RspCode = "97", Message = "Invalid signature" });
        }

        if (!int.TryParse(Request.Query["vnp_TxnRef"], NumberStyles.None, CultureInfo.InvariantCulture, out var paymentId) ||
            !long.TryParse(Request.Query["vnp_Amount"], NumberStyles.None, CultureInfo.InvariantCulture, out var amount))
        {
            return Json(new { RspCode = "01", Message = "Order not found" });
        }

        var succeeded = Request.Query["vnp_ResponseCode"] == "00" && Request.Query["vnp_TransactionStatus"] == "00";
        var transactionId = Request.Query["vnp_TransactionNo"].ToString();
        try
        {
            var result = await _workflow.ProcessVnpayCallbackAsync(paymentId, amount, transactionId, succeeded, cancellationToken);
            if (result == "00" && succeeded)
            {
                var payment = await _workflow.GetSuccessfulDepositForTicketAsync(paymentId, cancellationToken);
                var appointment = payment?.Appointment;
                if (payment != null && appointment != null && !string.IsNullOrWhiteSpace(appointment.QrCodeToken))
                {
                    var publicBaseUrl = _configuration["Vnpay:PublicBaseUrl"]?.TrimEnd('/') ?? $"{Request.Scheme}://{Request.Host}";
                    var ticketUrl = $"{publicBaseUrl}/Payment/Ticket/{Uri.EscapeDataString(appointment.QrCodeToken)}";
                    await _ticketEmailSender.SendAsync(
                        appointment.Patient.User.Email,
                        appointment.Patient.User.FullName,
                        $"{appointment.Doctor.AcademicTitle} {appointment.Doctor.User.FullName}",
                        appointment.AppointmentDate,
                        appointment.QrCodeToken,
                        ticketUrl,
                        cancellationToken);
                }
            }
            var message = result switch
            {
                "00" => "Confirm Success",
                "01" => "Order not found",
                "02" => "Order already confirmed",
                "04" => "Invalid amount or order state",
                _ => "Unknown error"
            };
            return Json(new { RspCode = result, Message = message });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "VNPAY IPN reconciliation failed for payment {PaymentId}", paymentId);
            return Json(new { RspCode = "99", Message = "Unknown error" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Ticket(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token)) return NotFound();
        var appointment = await _workflow.GetAppointmentByTicketTokenAsync(token, cancellationToken);
        return appointment == null ? NotFound() : View(new ETicketViewModel { Appointment = appointment });
    }

    private IEnumerable<KeyValuePair<string, string>> ReadVnpayParameters() =>
        Request.Query.SelectMany(pair => pair.Value.Select(value => new KeyValuePair<string, string>(pair.Key, value ?? string.Empty)));
}
