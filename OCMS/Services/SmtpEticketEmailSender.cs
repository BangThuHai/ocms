using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Services;

public sealed class SmtpEticketEmailSender : IEticketEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEticketEmailSender> _logger;

    public SmtpEticketEmailSender(IConfiguration configuration, ILogger<SmtpEticketEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string recipient, string patientName, string doctorName, DateTime appointmentDateUtc, string ticketToken, string ticketUrl, CancellationToken cancellationToken = default)
    {
        var host = _configuration["Smtp:Host"];
        var sender = _configuration["Smtp:From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(sender))
        {
            _logger.LogWarning("SMTP is not configured; the e-ticket remains available in the application notification and ticket page.");
            return false;
        }

        var port = int.TryParse(_configuration["Smtp:Port"], out var configuredPort) ? configuredPort : 587;
        using var message = new MailMessage(sender, recipient)
        {
            Subject = "Vé xác nhận lịch khám OCMS",
            IsBodyHtml = true,
            Body = $"""
                <div style="font-family:Arial,sans-serif;max-width:560px;margin:auto;color:#1f2937">
                  <h2 style="color:#0d6efd">OCMS Clinic - Vé khám</h2>
                  <p>Xin chào {HtmlEncoder.Default.Encode(patientName)}, lịch khám của bạn đã được xác nhận.</p>
                  <p><strong>Bác sĩ:</strong> {HtmlEncoder.Default.Encode(doctorName)}<br />
                  <strong>Thời gian:</strong> {appointmentDateUtc.AddHours(7):dd/MM/yyyy HH:mm} (GMT+7)</p>
                  <p>Mở vé điện tử để xem mã QR check-in:</p>
                  <p><a href="{HtmlEncoder.Default.Encode(ticketUrl)}">Mở vé điện tử</a></p>
                  <p>Mã vé: <code>{HtmlEncoder.Default.Encode(ticketToken)}</code></p>
                </div>
                """
        };

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = !bool.TryParse(_configuration["Smtp:EnableSsl"], out var enableSsl) || enableSsl
        };
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        if (!string.IsNullOrWhiteSpace(username))
        {
            client.Credentials = new NetworkCredential(username, password);
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await client.SendMailAsync(message);
            return true;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to send OCMS e-ticket to {Recipient}", recipient);
            return false;
        }
    }
}
