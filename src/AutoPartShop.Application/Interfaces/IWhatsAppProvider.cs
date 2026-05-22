namespace AutoPartShop.Application.Interfaces;

public interface IWhatsAppProvider
{
    Task<bool> SendAsync(string toPhone, string message, CancellationToken cancellationToken = default);
}
