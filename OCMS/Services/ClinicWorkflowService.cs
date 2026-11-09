using BusinessObjects.Entities;
using Repositories;

namespace Services;

public sealed class ClinicWorkflowService : IClinicWorkflowService
{
    private readonly IClinicWorkflowRepository _repository;

    public ClinicWorkflowService(IClinicWorkflowRepository repository) => _repository = repository;

    public Task<IReadOnlyList<Appointment>> GetBillingQueueAsync(CancellationToken cancellationToken = default) =>
        _repository.GetBillingQueueAsync(cancellationToken);

    public Task<Appointment?> GetBillingAppointmentAsync(int appointmentId, CancellationToken cancellationToken = default) =>
        _repository.GetBillingAppointmentAsync(appointmentId, cancellationToken);

    public Task<Payment> SettleAppointmentAsync(int appointmentId, string paymentMethod, int cashierId, CancellationToken cancellationToken = default) =>
        _repository.SettleAppointmentAsync(appointmentId, paymentMethod, cashierId, cancellationToken);

    public Task<Payment?> GetReceiptAsync(int paymentId, CancellationToken cancellationToken = default) =>
        _repository.GetReceiptAsync(paymentId, cancellationToken);

}