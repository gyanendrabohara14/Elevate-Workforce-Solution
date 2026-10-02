namespace ElevateWorkforce.Application.Interfaces;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string recipientEmail, string recipientName, CancellationToken cancellationToken = default);
}
