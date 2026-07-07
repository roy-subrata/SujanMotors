using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class AttendanceRepository : IAttendanceRepository
{
    private readonly AutoPartDbContext _dbContext;

    public AttendanceRepository(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AttendanceRecord?> GetAsync(Guid employeeId, DateTime date, CancellationToken cancellationToken = default)
    {
        var day = date.Date;
        return await _dbContext.AttendanceRecords
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.Date == day && !x.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<AttendanceRecord>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var day = date.Date;
        return await _dbContext.AttendanceRecords
            .Where(x => x.Date == day && !x.Isdeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AttendanceRecord>> GetByEmployeeMonthAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken = default)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        return await _dbContext.AttendanceRecords
            .Where(x => x.EmployeeId == employeeId && x.Date >= start && x.Date < end && !x.Isdeleted)
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertRangeAsync(IEnumerable<AttendanceRecord> records, string modifiedBy, CancellationToken cancellationToken = default)
    {
        var incoming = records.ToList();
        if (incoming.Count == 0) return;

        var dates = incoming.Select(r => r.Date).Distinct().ToList();
        var employeeIds = incoming.Select(r => r.EmployeeId).Distinct().ToList();

        var existing = await _dbContext.AttendanceRecords
            .Where(x => dates.Contains(x.Date) && employeeIds.Contains(x.EmployeeId) && !x.Isdeleted)
            .ToListAsync(cancellationToken);

        foreach (var record in incoming)
        {
            var match = existing.FirstOrDefault(x => x.EmployeeId == record.EmployeeId && x.Date == record.Date);
            if (match is null)
            {
                record.CreatedBy = modifiedBy;
                record.ModifiedBy = modifiedBy;
                await _dbContext.AttendanceRecords.AddAsync(record, cancellationToken);
            }
            else
            {
                match.Mark(record.Status, record.CheckInTime, record.CheckOutTime, record.Notes);
                match.ModifiedBy = modifiedBy;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
