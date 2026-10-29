using BusinessObjects.Entities;

namespace Services;

public interface IClinicWorkflowService
{
    Task<IReadOnlyList<Appointment>> GetBillingQueueAsync(CancellationToken cancellationToken = default);
    Task<Appointment?> GetBillingAppointmentAsync(int appointmentId, CancellationToken cancellationToken = default);
    Task<Payment> SettleAppointmentAsync(int appointmentId, string paymentMethod, int cashierId, CancellationToken cancellationToken = default);
    Task<Payment?> GetReceiptAsync(int paymentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Appointment>> GetPharmacyQueueAsync(CancellationToken cancellationToken = default);
    Task<Appointment?> GetPharmacyAppointmentAsync(int appointmentId, CancellationToken cancellationToken = default);
    Task DispenseAppointmentAsync(int appointmentId, int staffId, CancellationToken cancellationToken = default);
    Task<Payment> CreatePendingDepositAsync(int appointmentId, int patientUserId, CancellationToken cancellationToken = default);
    Task<string> ProcessVnpayCallbackAsync(int paymentId, long amountInCents, string? gatewayTransactionId, bool succeeded, CancellationToken cancellationToken = default);
    Task<Payment?> GetPaymentStatusAsync(int paymentId, int patientUserId, CancellationToken cancellationToken = default);
    Task<Payment?> GetSuccessfulDepositForTicketAsync(int paymentId, CancellationToken cancellationToken = default);
    Task<Appointment?> GetAppointmentByTicketTokenAsync(string token, CancellationToken cancellationToken = default);
}
