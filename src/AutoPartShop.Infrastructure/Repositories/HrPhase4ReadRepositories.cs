using AutoPartShop.Application.Hr;
using AutoPartShop.Application.Hr.Dtos;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories
{
    public class SalaryAdvanceReadRepository(AutoPartDbContext _dbContext) : ISalaryAdvanceReadRepository
    {
        public async Task<(IReadOnlyCollection<SalaryAdvanceResponse> responses, int totalCount)> FindAllQuery(SalaryAdvanceQuery query, CancellationToken cancellationToken)
        {
            var search = query.Search.ToLower();

            var advances =
                from a in _dbContext.SalaryAdvances
                join e in _dbContext.Employees on a.EmployeeId equals e.Id
                where !a.Isdeleted
                select new { a, e };

            if (!string.IsNullOrWhiteSpace(search))
            {
                advances = advances.Where(x =>
                    EF.Functions.Like(x.e.Name, $"%{search}%") ||
                    EF.Functions.Like(x.e.EmployeeCode, $"%{search}%") ||
                    EF.Functions.Like(x.a.Notes, $"%{search}%"));
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                advances = advances.Where(x => x.a.Status == query.Status);
            }

            if (query.EmployeeId is Guid employeeId)
            {
                advances = advances.Where(x => x.a.EmployeeId == employeeId);
            }

            advances = advances.OrderByDescending(x => x.a.AdvanceDate);

            var totalCount = await advances.CountAsync(cancellationToken);
            var items = await advances
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(x => new SalaryAdvanceResponse
                {
                    Id = x.a.Id,
                    EmployeeId = x.a.EmployeeId,
                    EmployeeCode = x.e.EmployeeCode,
                    EmployeeName = x.e.Name,
                    AdvanceDate = x.a.AdvanceDate,
                    Amount = x.a.Amount,
                    PaymentMethod = x.a.PaymentMethod,
                    Notes = x.a.Notes,
                    Status = x.a.Status,
                    SettledAt = x.a.SettledAt,
                    SettledRunCode = _dbContext.PayrollRuns
                        .Where(r => r.Id == x.a.SettledPayrollRunId)
                        .Select(r => r.RunCode)
                        .FirstOrDefault(),
                    CreatedAt = x.a.CreatedDate
                })
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
    }

    public class HrSalesReadRepository(AutoPartDbContext _dbContext) : IHrSalesReadRepository
    {
        public async Task<IReadOnlyDictionary<Guid, decimal>> GetMonthlySalesTotalsByEmployee(int year, int month, CancellationToken cancellationToken)
        {
            var start = new DateTime(year, month, 1);
            var end = start.AddMonths(1);

            var totals = await (
                from e in _dbContext.Employees
                join u in _dbContext.Users on e.UserId equals u.Id
                join o in _dbContext.SalesOrders on u.UserName equals o.CreatedBy
                where !e.Isdeleted && !o.Isdeleted
                    && o.SODate >= start && o.SODate < end
                    && o.Status != "CANCELLED" && o.Status != "RETURNED"
                group o by e.Id into g
                select new { EmployeeId = g.Key, Total = g.Sum(o => o.TotalAmount + o.TaxAmount) })
                .ToListAsync(cancellationToken);

            return totals.ToDictionary(x => x.EmployeeId, x => x.Total);
        }
    }
}
