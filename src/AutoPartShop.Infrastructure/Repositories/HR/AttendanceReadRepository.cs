using AutoPartShop.Application.HR;
using AutoPartShop.Application.HR.Dtos;
using AutoPartShop.Domain.Enums.HR;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories.HR
{
    public class AttendanceReadRepository(AutoPartDbContext _dbContext) : IAttendanceReadRepository
    {
        public async Task<IReadOnlyCollection<DailyAttendanceRow>> GetDailySheet(DateTime date, CancellationToken cancellationToken)
        {
            var day = date.Date;

            // Declared holidays live in their own table and were never consulted here, so a
            // holiday only showed up if somebody had also hand-marked every employee HOLIDAY.
            var holiday = await _dbContext.Holidays
                .Where(h => h.Date == day && !h.Isdeleted)
                .Select(h => h.Name)
                .FirstOrDefaultAsync(cancellationToken);

            var rows = await _dbContext.Employees
                .Where(e => !e.Isdeleted && e.Status == EmployeeStatus.ACTIVE)
                .OrderBy(e => e.Name)
                .Select(e => new
                {
                    Employee = e,
                    Record = _dbContext.AttendanceRecords
                        .Where(a => a.EmployeeId == e.Id && a.Date == day && !a.Isdeleted)
                        .FirstOrDefault(),
                    ShiftName = _dbContext.Shifts
                        .Where(s => s.Id == e.ShiftId && !s.Isdeleted)
                        .Select(s => s.Name)
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
                ShiftName = x.ShiftName,
                IsMarked = x.Record != null,
                IsHoliday = holiday is not null,
                HolidayName = holiday,
                Status = x.Record != null ? x.Record.Status : null,
                CheckInTime = x.Record != null ? x.Record.CheckInTime : null,
                CheckOutTime = x.Record != null ? x.Record.CheckOutTime : null,
                Notes = x.Record != null ? x.Record.Notes : string.Empty
            }).ToList();
        }

        public async Task<IReadOnlyCollection<MonthlyAttendanceSummaryRow>> GetMonthlySummary(int year, int month, CancellationToken cancellationToken)
        {
            var start = new DateTime(year, month, 1);
            var end = start.AddMonths(1);

            // Days declared as holidays for the whole company. holidayDays used to count only
            // manually-marked HOLIDAY records, so a declared holiday reported 0.
            var declaredHolidays = await _dbContext.Holidays
                .Where(h => h.Date >= start && h.Date < end && !h.Isdeleted)
                .Select(h => h.Date)
                .Distinct()
                .ToListAsync(cancellationToken);

            // Dates an employee was hand-marked HOLIDAY, so a day that is both is not counted twice.
            var markedHolidayDates = await _dbContext.AttendanceRecords
                .Where(a => a.Date >= start && a.Date < end && !a.Isdeleted
                         && a.Status == AttendanceStatus.HOLIDAY)
                .Select(a => new { a.EmployeeId, a.Date })
                .ToListAsync(cancellationToken);

            var markedHolidaysByEmployee = markedHolidayDates
                .GroupBy(x => x.EmployeeId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Date).ToHashSet());

            var counts = await _dbContext.AttendanceRecords
                .Where(a => a.Date >= start && a.Date < end && !a.Isdeleted)
                .GroupBy(a => a.EmployeeId)
                .Select(g => new
                {
                    EmployeeId = g.Key,
                    Present = g.Count(a => a.Status == AttendanceStatus.PRESENT),
                    Late = g.Count(a => a.Status == AttendanceStatus.LATE),
                    Half = g.Count(a => a.Status == AttendanceStatus.HALF_DAY),
                    Absent = g.Count(a => a.Status == AttendanceStatus.ABSENT),
                    Leave = g.Count(a => a.Status == AttendanceStatus.LEAVE),
                    Holiday = g.Count(a => a.Status == AttendanceStatus.HOLIDAY),
                    Total = g.Count()
                })
                .ToListAsync(cancellationToken);

            var employees = await _dbContext.Employees
                .Where(e => !e.Isdeleted && e.Status == EmployeeStatus.ACTIVE)
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
                    HolidayDays = markedHolidaysByEmployee.TryGetValue(e.Id, out var marked)
                        ? marked.Union(declaredHolidays).Count()
                        : declaredHolidays.Count,
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

            if (query.Status is not null)
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
