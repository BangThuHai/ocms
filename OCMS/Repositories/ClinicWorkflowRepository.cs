using System.Data;
using BusinessObjects.Entities;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;

namespace Repositories;

public sealed class ClinicWorkflowRepository : IClinicWorkflowRepository
{
    private const string CompletedAppointment = "Completed";
    private readonly OcmsDbContext _db;

    public ClinicWorkflowRepository(OcmsDbContext db) => _db = db;

    public async Task<IReadOnlyList<Appointment>> GetBillingQueueAsync(CancellationToken cancellationToken = default)
    {
        return await BillingAppointmentQuery()
            .Where(a => a.Status == CompletedAppointment &&
                        !_db.Payments.Any(p => p.AppointmentId == a.Id &&
                                               p.PaymentType == "FinalSettlement" &&
                                               p.PaymentStatus == "Success"))
            .OrderBy(a => a.AppointmentDate)
            .ToListAsync(cancellationToken);
    }

    public Task<Appointment?> GetBillingAppointmentAsync(int appointmentId, CancellationToken cancellationToken = default) =>
        BillingAppointmentQuery().SingleOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

    public async Task<Payment> SettleAppointmentAsync(int appointmentId, string paymentMethod, int cashierId, CancellationToken cancellationToken = default)
    {
        if (paymentMethod is not ("Cash" or "POS"))
        {
            throw new InvalidOperationException("Phương thức thanh toán không hợp lệ.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var appointment = await BillingAppointmentQuery(tracking: true)
            .SingleOrDefaultAsync(a => a.Id == appointmentId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy lịch hẹn cần quyết toán.");

        if (appointment.Status != CompletedAppointment)
        {
            throw new InvalidOperationException("Chỉ quyết toán lượt khám đã hoàn tất.");
        }

        var alreadySettled = await _db.Payments.AnyAsync(p => p.AppointmentId == appointmentId &&
            p.PaymentType == "FinalSettlement" && p.PaymentStatus == "Success", cancellationToken);
        if (alreadySettled)
        {
            throw new InvalidOperationException("Lượt khám này đã được quyết toán.");
        }

        var consultationFee = appointment.ConsultationFee ?? appointment.Doctor.PricePerSlot;
        var completedServices = appointment.ServiceOrders.Where(s => s.Status == "Completed").ToList();
        var unpaidPrescriptionLines = appointment.MedicalRecord?.PrescriptionDetails
            .Where(p => p.Status == "Pending")
            .ToList() ?? new List<PrescriptionDetail>();
        var deposit = appointment.Payments
            .Where(p => p.PaymentType == "Deposit" && p.PaymentStatus == "Success")
            .Sum(p => p.Amount);
        var total = consultationFee + completedServices.Sum(s => s.UnitPrice) +
                    unpaidPrescriptionLines.Sum(p => p.Quantity * p.UnitPrice);
        var remaining = Math.Max(0m, total - deposit);

        var payment = new Payment
        {
            AppointmentId = appointmentId,
            Amount = remaining,
            PaymentMethod = paymentMethod,
            PaymentStatus = "Success",
            PaymentType = "FinalSettlement",
            CashierId = cashierId,
            CreatedAt = DateTime.UtcNow
        };
        _db.Payments.Add(payment);

        foreach (var line in unpaidPrescriptionLines)
        {
            line.Status = "Paid";
        }
        foreach (var serviceOrder in completedServices)
        {
            serviceOrder.Status = "Paid";
        }

        await _db.SaveChangesAsync(cancellationToken);
        payment.TransactionId = $"RCPT-{payment.Id:D8}";
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return payment;
    }

    public Task<Payment?> GetReceiptAsync(int paymentId, CancellationToken cancellationToken = default) =>
        _db.Payments
            .AsNoTracking()
            .Include(p => p.Cashier)
            .Include(p => p.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
            .Include(p => p.Appointment).ThenInclude(a => a.Doctor).ThenInclude(d => d.User)
            .Include(p => p.Appointment).ThenInclude(a => a.Doctor).ThenInclude(d => d.Department)
            .Include(p => p.Appointment).ThenInclude(a => a.MedicalRecord).ThenInclude(m => m!.PrescriptionDetails).ThenInclude(d => d.Medicine)
            .Include(p => p.Appointment).ThenInclude(a => a.ServiceOrders).ThenInclude(s => s.MedicalService)
            .SingleOrDefaultAsync(p => p.Id == paymentId && p.PaymentType == "FinalSettlement", cancellationToken);


    private IQueryable<Appointment> BillingAppointmentQuery(bool tracking = false)
    {
        IQueryable<Appointment> query = _db.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .Include(a => a.Doctor).ThenInclude(d => d.Department)
            .Include(a => a.Payments)
            .Include(a => a.ServiceOrders).ThenInclude(s => s.MedicalService)
            .Include(a => a.MedicalRecord).ThenInclude(m => m!.PrescriptionDetails).ThenInclude(p => p.Medicine);
        return tracking ? query : query.AsNoTracking();
    }
}