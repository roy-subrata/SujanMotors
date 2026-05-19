using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;


public interface IPaymentProviderRepository : IBaseRepository<PaymentProvider>
{
    Task<PaymentProvider?> GetByNameAsync(string providerName, CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentProvider>> GetByTypeAsync(string providerType, CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentProvider>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<PaymentProvider?> GetDefaultAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentProvider>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<PaymentProvider> providers, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
