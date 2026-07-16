using AutoPartShop.Application.Hr;
using AutoPartShop.Application.Hr.Dtos;
using AutoPartsShop.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories
{
    public class EmployeeReadRepository(AutoPartDbContext _dbContext) : IEmployeeReadRepository
    {
        public async Task<(IReadOnlyCollection<EmployeeResponse> responses, int totalCount)> FindAllQuery(EmployeeQuery query, CancellationToken cancellationToken)
        {
            var search = query.Search.ToLower();

            var employees = _dbContext.Employees.Where(x => !x.Isdeleted);

            if (!string.IsNullOrWhiteSpace(search))
            {
                employees = employees
                    .Where(x =>
                        EF.Functions.Like(x.EmployeeCode, $"%{search}%") ||
                        EF.Functions.Like(x.Name, $"%{search}%") ||
                        EF.Functions.Like(x.Phone, $"%{search}%") ||
                        EF.Functions.Like(x.Email, $"%{search}%") ||
                        EF.Functions.Like(x.Designation, $"%{search}%"));
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                employees = employees.Where(x => x.Status == query.Status);
            }

            if (!string.IsNullOrWhiteSpace(query.Department))
            {
                employees = employees.Where(x => x.Department == query.Department);
            }

            if (query.Sorts != null && query.Sorts.Any())
            {
                var sorts =
                    query.Sorts.Select(x => (x.Field, x.Direction == "asc" ? true : false)).ToArray();
                employees = employees.OrderByMultiple(sorts);
            }
            else
            {
                employees = employees.OrderByDescending(x => x.CreatedDate);
            }

            var totalCount = await employees.CountAsync(cancellationToken);
            var items = await employees
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(e => new EmployeeResponse
                {
                    Id = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    Name = e.Name,
                    Phone = e.Phone,
                    Email = e.Email,
                    NidNumber = e.NidNumber,
                    DateOfBirth = e.DateOfBirth,
                    Gender = e.Gender,
                    Address = e.Address,
                    City = e.City,
                    Designation = e.Designation,
                    Department = e.Department,
                    JoinDate = e.JoinDate,
                    EndDate = e.EndDate,
                    EmploymentType = e.EmploymentType,
                    MonthlySalary = e.MonthlySalary,
                    Currency = e.Currency,
                    ShiftId = e.ShiftId,
                    ShiftName = _dbContext.Shifts
                        .Where(s => s.Id == e.ShiftId)
                        .Select(s => s.Name)
                        .FirstOrDefault(),
                    MonthlyTaxDeduction = e.MonthlyTaxDeduction,
                    CommissionRate = e.CommissionRate,
                    EmergencyContactName = e.EmergencyContactName,
                    EmergencyContactPhone = e.EmergencyContactPhone,
                    Status = e.Status,
                    Notes = e.Notes,
                    PhotoUrl = e.PhotoUrl,
                    UserId = e.UserId,
                    UserName = _dbContext.Users
                        .Where(u => u.Id == e.UserId)
                        .Select(u => u.UserName)
                        .FirstOrDefault(),
                    CreatedAt = e.CreatedDate
                })
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<IReadOnlyCollection<LinkableUserResponse>> GetLinkableUsers(Guid? currentEmployeeId, CancellationToken cancellationToken)
        {
            // Staff accounts only (online shoppers have CustomerId set), active, and not
            // already backing another employee record
            var linkedUserIds = _dbContext.Employees
                .Where(e => !e.Isdeleted && e.UserId != null && e.Id != currentEmployeeId)
                .Select(e => e.UserId!.Value);

            return await _dbContext.Users
                .Where(u => u.CustomerId == null && u.IsActive && !linkedUserIds.Contains(u.Id))
                .OrderBy(u => u.UserName)
                .Select(u => new LinkableUserResponse
                {
                    Id = u.Id,
                    UserName = u.UserName ?? string.Empty,
                    FullName = (u.FirstName + " " + u.LastName).Trim(),
                    Email = u.Email ?? string.Empty
                })
                .ToListAsync(cancellationToken);
        }
    }
}
