using AutoPartShop.Domain.Enums.HR;

namespace AutoPartShop.Domain.Entities.HR;

/// <summary>
/// One employee's attendance for one calendar day. Marked manually by Admin/Manager
/// (no biometric device integration in v1). Unique per (EmployeeId, Date).
/// </summary>
public class AttendanceRecord : AuditableEntity
{
    public Guid EmployeeId { get; private set; }
    public DateTime Date { get; private set; }
    public TimeSpan? CheckInTime { get; private set; }
    public TimeSpan? CheckOutTime { get; private set; }
    public AttendanceStatus Status { get; private set; } = AttendanceStatus.PRESENT;
    public string Notes { get; private set; } = string.Empty;

    private AttendanceRecord() { }

    public static AttendanceRecord Create(Guid employeeId, DateTime date, AttendanceStatus status,
        TimeSpan? checkInTime = null, TimeSpan? checkOutTime = null, string notes = "")
    {
        if (employeeId == Guid.Empty)
            throw new ArgumentException("EmployeeId cannot be empty", nameof(employeeId));

        if (date == default)
            throw new ArgumentException("Date is required", nameof(date));

        var record = new AttendanceRecord
        {
            EmployeeId = employeeId,
            Date = date.Date
        };
        record.Mark(status, checkInTime, checkOutTime, notes);
        return record;
    }

    public void Mark(AttendanceStatus status, TimeSpan? checkInTime, TimeSpan? checkOutTime, string notes)
    {
        if (checkInTime.HasValue && checkOutTime.HasValue && checkOutTime < checkInTime)
            throw new ArgumentException("Check-out time cannot be before check-in time", nameof(checkOutTime));

        Status = status;
        CheckInTime = checkInTime;
        CheckOutTime = checkOutTime;
        Notes = notes?.Trim() ?? string.Empty;
    }
}
