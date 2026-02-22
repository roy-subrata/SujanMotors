using AutoPartShop.Application.Customers.Dtos;


namespace AutoPartShop.Application.Customers
{
    public interface ICustomerReadRepository
    {
        Task<(IEnumerable<CustomerResponse> customers, int totalCount)> FindAllyAsync(CustomerQuery query, CancellationToken cancellationToken = default);
    }
}


