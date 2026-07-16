namespace AutoPartShop.Domain.Entities;

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
    public string Status { get; private set; } = "PRESENT";  // PRESENT, LATE, HALF_DAY, ABSENT, LEAVE, HOLIDAY
    public string Notes { get; private set; } = string.Empty;

    private static readonly string[] ValidStatuses = ["PRESENT", "LATE", "HALF_DAY", "ABSENT", "LEAVE", "HOLIDAY"];

    private AttendanceRecord() { }

    public static AttendanceRecord Create(Guid employeeId, DateTime date, string status,
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

    public void Mark(string status, TimeSpan? checkInTime, TimeSpan? checkOutTime, string notes)
    {
        var normalized = status?.Trim().ToUpper() ?? string.Empty;
        if (!ValidStatuses.Contains(normalized))
            throw new ArgumentException($"Invalid attendance status '{status}'", nameof(status));

        if (checkInTime.HasValue && checkOutTime.HasValue && checkOutTime < checkInTime)
            throw new ArgumentException("Check-out time cannot be before check-in time", nameof(checkOutTime));

        Status = normalized;
        CheckInTime = checkInTime;
        CheckOutTime = checkOutTime;
        Notes = notes?.Trim() ?? string.Empty;
    }
}
