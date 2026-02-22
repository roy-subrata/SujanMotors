using AutoPartShop.Application.Customers;
using AutoPartShop.Application.Customers.Dtos;
using AutoPartsShop.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class CustomerReadRepository(AutoPartDbContext _dbContext) : ICustomerReadRepository
{
    public async Task<(IEnumerable<CustomerResponse> customers, int totalCount)> FindAllyAsync(CustomerQuery query, CancellationToken cancellationToken = default)
    {
        var term = query.Search.ToLower();
        var customers = _dbContext.Customers
            .Include(x => x.CustomerPayments)
            .Where(x => !x.Isdeleted && (
             (EF.Functions.Like(x.FirstName, $"%{term}%") ||
             EF.Functions.Like(x.LastName, $"%{term}%") ||
             EF.Functions.Like(x.CompanyName, $"%{term}%") ||
             EF.Functions.Like(x.Email, $"%{term}%") ||
             EF.Functions.Like(x.CustomerCode, $"%{term}%") ||
             EF.Functions.Like(x.CustomerType, $"%{term}%") ||
             EF.Functions.Like(x.City, $"%{term}%")
            )));

        if (!string.IsNullOrWhiteSpace(query.CustomerType))
        {
            customers = customers.Where(x => x.CustomerType.Equals(query.CustomerType));
        }

        if (query.Sorts != null && query.Sorts.Any())
        {
            var sorts =
                query.Sorts.Select(x => (x.Field, x.Direction == "asc" ? true : false)).ToArray();
            customers = customers.OrderByMultiple(sorts);
        }
        else
        {
            customers = customers.OrderBy(x => x.CreatedDate);
        }

        var totalCount = await customers.CountAsync(cancellationToken);
        var items = await customers
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
             .Select(customer=>new CustomerResponse
             {
                 Id = customer.Id,
                 CustomerCode = customer.CustomerCode,
                 FirstName = customer.FirstName,
                 LastName = customer.LastName,
                 FullName = customer.GetFullName(),
                 Email = customer.Email,
                 Phone = customer.Phone,
                 AlternatePhone = customer.AlternatePhone,
                 CompanyName = customer.CompanyName,
                 BillingAddress = customer.BillingAddress,
                 ShippingAddress = customer.ShippingAddress,
                 City = customer.City,
                 State = customer.State,
                 PostalCode = customer.PostalCode,
                 Country = customer.Country,
                 CustomerType = customer.CustomerType,
                 Status = customer.Status,
                 CurrentBalance = customer.CurrentBalance,
                 AdvanceAmount = customer.AdvanceAmount,  // Available advance credit (sum of RemainingAmount)
                 DueAmount = customer.CurrentBalance,      // Outstanding balance (due)
                 CanPlaceOrder = customer.CanPlaceOrder(),
                 PrimaryContactPerson = customer.PrimaryContactPerson,
                 LastPurchaseDate = customer.LastPurchaseDate,
                 TotalPurchaseAmount = customer.TotalPurchaseAmount,
                 Notes = customer.Notes,
                 CreatedAt = DateTime.UtcNow

             })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
