using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;

public interface ICustomerRepository : IBaseRepository<Customer>
{
    Task<Customer?> GetByCodeAsync(string customerCode, CancellationToken cancellationToken = default);
    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> GetByTypeAsync(string customerType, CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> GetByCityAsync(string city, CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> GetByCountryAsync(string country, CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> GetWithCreditLimitExceededAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<Customer> customers, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> GetRecentAsync(int limit, CancellationToken cancellationToken = default);
    Task<Customer?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default);
}


