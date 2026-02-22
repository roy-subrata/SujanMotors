using AutoPartShop.Application.DTOs.CustomerDtos;

namespace AutoPartShop.Api.Services;

public interface ICustomerAccountSummaryService
{
    Task<CustomerAccountSummaryDto> GetAccountSummaryAsync(
        CustomerAccountSummaryQuery query,
        CancellationToken ct = default);
}
