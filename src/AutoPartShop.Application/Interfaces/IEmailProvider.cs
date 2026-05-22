namespace AutoPartShop.Application.Interfaces;

public interface IEmailProvider
{
    Task<bool> SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
