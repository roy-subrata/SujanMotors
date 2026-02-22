using AutoPartShop.Application.CustomerPayment.Dtos;

namespace AutoPartShop.Application.CustomerPayment;

public interface ICustomerPaymentReadRepository
{
    Task<(IEnumerable<CustomerPaymentResponse> payments, int totalCount)> FindAllAsync(CustomerPaymentQuery query, CancellationToken cancellationToken = default);

}