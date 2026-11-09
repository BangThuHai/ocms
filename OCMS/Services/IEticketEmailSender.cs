namespace Services;

public interface IEticketEmailSender
{
    Task<bool> SendAsync(string recipient, string patientName, string doctorName, DateTime appointmentDateUtc, string ticketToken, string ticketUrl, CancellationToken cancellationToken = default);
}
