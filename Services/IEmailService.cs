namespace BarberMenagment.Services;

public interface IEmailService
{
    Task<bool> SendAsync(
        string recipientEmail,
        string recipientName,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
