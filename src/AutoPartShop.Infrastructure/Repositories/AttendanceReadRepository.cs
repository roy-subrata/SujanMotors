using AutoPartShop.Application.Hr;
using AutoPartShop.Application.Hr.Dtos;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories
{
    public class AttendanceReadRepository(AutoPartDbContext _dbContext) : IAttendanceReadRepository
    {
        public async Task<IReadOnlyCollection<DailyAttendanceRow>> GetDailySheet(DateTime date, CancellationToken cancellationToken)
        {
            var day = date.Date;

            var rows = await _dbContext.Employees
                .Where(e => !e.Isdeleted && e.Status == "ACTIVE")
                .OrderBy(e => e.Name)
                .Select(e => new
                {
                    Employee = e,
                    Record = _dbContext.AttendanceRecords
                        .Where(a => a.EmployeeId == e.Id && a.Date == day && !a.Isdeleted)
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

            return rows.Select(x => new DailyAttendanceRow
            {
                EmployeeId = x.Employee.Id,
                EmployeeCode = x.Employee.EmployeeCode,
                Name = x.Employee.Name,
                Designation = x.Employee.Designation,
                Department = x.Employee.Department,
                IsMarked = x.Record != null,
                Status = x.Record != null ? x.Record.Status : string.Empty,
                CheckInTime = x.Record != null ? x.Record.CheckInTime : null,
                CheckOutTime = x.Record != null ? x.Record.CheckOutTime : null,
                Notes = x.Record != null ? x.Record.Notes : string.Empty
            }).ToList();
        }

        public async Task<IReadOnlyCollection<MonthlyAttendanceSummaryRow>> GetMonthlySummary(int year, int month, CancellationToken cancellationToken)
        {
            var start = new DateTime(year, month, 1);
            var end = start.AddMonths(1);

            var counts = await _dbContext.AttendanceRecords
                .Where(a => a.Date >= start && a.Date < end && !a.Isdeleted)
                .GroupBy(a => a.EmployeeId)
                .Select(g => new
                {
                    EmployeeId = g.Key,
                    Present = g.Count(a => a.Status == "PRESENT"),
                    Late = g.Count(a => a.Status == "LATE"),
                    Half = g.Count(a => a.Status == "HALF_DAY"),
                    Absent = g.Count(a => a.Status == "ABSENT"),
                    Leave = g.Count(a => a.Status == "LEAVE"),
                    Holiday = g.Count(a => a.Status == "HOLIDAY"),
                    Total = g.Count()
                })
                .ToListAsync(cancellationToken);

            var employees = await _dbContext.Employees
                .Where(e => !e.Isdeleted && e.Status == "ACTIVE")
                .OrderBy(e => e.Name)
                .Select(e => new { e.Id, e.EmployeeCode, e.Name, e.Department })
                .ToListAsync(cancellationToken);

            return employees.Select(e =>
            {
                var c = counts.FirstOrDefault(x => x.EmployeeId == e.Id);
                return new MonthlyAttendanceSummaryRow
                {
                    EmployeeId = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    Name = e.Name,
                    Department = e.Department,
                    PresentDays = c?.Present ?? 0,
                    LateDays = c?.Late ?? 0,
                    HalfDays = c?.Half ?? 0,
                    AbsentDays = c?.Absent ?? 0,
                    LeaveDays = c?.Leave ?? 0,
                    HolidayDays = c?.Holiday ?? 0,
                    MarkedDays = c?.Total ?? 0
                };
            }).ToList();
        }
    }

    public class LeaveRequestReadRepository(AutoPartDbContext _dbContext) : ILeaveRequestReadRepository
    {
        public async Task<(IReadOnlyCollection<LeaveRequestResponse> responses, int totalCount)> FindAllQuery(LeaveRequestQuery query, CancellationToken cancellationToken)
        {
            var search = query.Search.ToLower();

            var requests =
                from l in _dbContext.LeaveRequests
                join e in _dbContext.Employees on l.EmployeeId equals e.Id
                where !l.Isdeleted
                select new { l, e };

            if (!string.IsNullOrWhiteSpace(search))
            {
                requests = requests.Where(x =>
                    EF.Functions.Like(x.e.Name, $"%{search}%") ||
                    EF.Functions.Like(x.e.EmployeeCode, $"%{search}%") ||
                    EF.Functions.Like(x.l.Reason, $"%{search}%"));
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                requests = requests.Where(x => x.l.Status == query.Status);
            }

            if (query.EmployeeId is Guid employeeId)
            {
                requests = requests.Where(x => x.l.EmployeeId == employeeId);
            }

            requests = requests.OrderByDescending(x => x.l.CreatedDate);

            var totalCount = await requests.CountAsync(cancellationToken);
            var items = await requests
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(x => new LeaveRequestResponse
                {
                    Id = x.l.Id,
                    EmployeeId = x.l.EmployeeId,
                    EmployeeCode = x.e.EmployeeCode,
                    EmployeeName = x.e.Name,
                    LeaveType = x.l.LeaveType,
                    FromDate = x.l.FromDate,
                    ToDate = x.l.ToDate,
                    TotalDays = x.l.TotalDays,
                    Reason = x.l.Reason,
                    Status = x.l.Status,
                    DecisionBy = x.l.DecisionBy,
                    DecisionAt = x.l.DecisionAt,
                    DecisionNotes = x.l.DecisionNotes,
                    CreatedAt = x.l.CreatedDate
                })
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
    }
}
