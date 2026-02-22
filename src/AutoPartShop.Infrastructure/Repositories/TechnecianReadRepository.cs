using AutoPartShop.Application.Technecians;
using AutoPartShop.Application.Technecians.Dtos;
using AutoPartsShop.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories
{
    public class TechnecianReadRepository(AutoPartDbContext _dbContext) : ITechnecianReadRepository
    {
        public async Task<(IReadOnlyCollection<TechnicianResponse> responses, int totalCount)> FindAllQuery(TechnecianQuery query, CancellationToken cancellationToken)
        {
            var search = query.Search.ToLower();

            var technicians = _dbContext.Technicians.Where(x => !x.Isdeleted);

            if (!string.IsNullOrWhiteSpace(search))
            {
                technicians = technicians
                    .Where(x =>
               (EF.Functions.Like(x.TechnicianCode, $"%{search}%") ||
               EF.Functions.Like(x.Name, $"%{search}%") ||
               EF.Functions.Like(x.Phone, $"%{search}%") ||
               EF.Functions.Like(x.Email, $"%{search}%")
              ));
            }
            if (query.Sorts != null && query.Sorts.Any())
            {
                var sorts =
                    query.Sorts.Select(x => (x.Field, x.Direction == "asc" ? true : false)).ToArray();
                technicians = technicians.OrderByMultiple(sorts);
            }
            else
            {
                technicians = technicians.OrderBy(x => x.CreatedDate);
            }

            var totalCount = await technicians.CountAsync(cancellationToken);
            var items = await technicians
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                 .Select(t => new TechnicianResponse
                 {

                     Id = t.Id,
                     TechnicianCode = t.TechnicianCode,
                     Name = t.Name,
                     Phone = t.Phone,
                     Email = t.Email,
                     ShopName = t.ShopName,
                     Address = t.Address,
                     City = t.City,
                     Status = t.Status,
                     Notes = t.Notes,
                     CreatedAt = t.CreatedDate

                 })
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
    }
}
