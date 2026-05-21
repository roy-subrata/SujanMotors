namespace AutoPartShop.Application.Interfaces;

public interface ISmsProvider
{
    Task<bool> SendAsync(string toPhone, string message, CancellationToken cancellationToken = default);
}
