using AutoPartShop.Application.Suppliers;
using AutoPartShop.Application.Suppliers.Dtos;
using AutoPartShop.Domain.Entities;
using AutoPartsShop.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories
{
    public class SupplierReadRepository(AutoPartDbContext _db) : ISupplierReadRepository
    {

        public async Task<(IEnumerable<SupplierResponse> Suppliers, int TotalCount)> FindAllAsynce(SupplierQuery query, CancellationToken cancellationToken = default)
        {
            var term = query.Search.ToLower();
            var suppliers = _db.Suppliers
                .Include(x => x.SupplierPayments)
                .Where(x => !x.Isdeleted && (
                 (EF.Functions.Like(x.Name, $"%{term}%") ||
                 EF.Functions.Like(x.Country, $"%{term}%") ||
                 EF.Functions.Like(x.Phone, $"%{term}%") ||
                 EF.Functions.Like(x.Email, $"%{term}%") ||
                 EF.Functions.Like(x.City, $"%{term}%")
                )));


            if (query.Sorts != null && query.Sorts.Any())
            {
                var sorts =
                    query.Sorts.Select(x => (x.Field, x.Direction == "asc" ? true : false)).ToArray();
                suppliers = suppliers.OrderByMultiple(sorts);
            }
            else
            {
                suppliers = suppliers.OrderBy(x => x.CreatedDate);
            }

            var totalCount = await suppliers.CountAsync(cancellationToken);
            var items = await suppliers
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(supplier=>new SupplierResponse
                {
                    Id = supplier.Id,
                    Name = supplier.Name,
                    Code = supplier.Code,
                    ContactPerson = supplier.ContactPerson,
                    Email = supplier.Email,
                    Phone = supplier.Phone,
                    Address = supplier.Address,
                    City = supplier.City,
                    State = supplier.State,
                    Country = supplier.Country,
                    PostalCode = supplier.PostalCode,
                    CurrentBalance = supplier.CurrentBalance,
                    IsActive = supplier.IsActive,
                    Rating = supplier.Rating,
                    CreatedBy = supplier.CreatedBy,
                    ModifiedBy = supplier.ModifiedBy
                })
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
    }
}
