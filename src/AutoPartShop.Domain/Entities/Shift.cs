namespace AutoPartShop.Domain.Entities;

/// <summary>
/// A work shift (e.g. "Morning 9-6"). Assigned to employees; the attendance punch
/// endpoint uses the shift start + grace period to auto-flag LATE check-ins.
/// </summary>
public class Shift : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public TimeSpan StartTime { get; private set; }
    public TimeSpan EndTime { get; private set; }
    public int GraceMinutes { get; private set; }  // Minutes after StartTime before a check-in counts as LATE
    public string Notes { get; private set; } = string.Empty;

    private Shift() { }

    public static Shift Create(string name, TimeSpan startTime, TimeSpan endTime, int graceMinutes = 10, string notes = "")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time", nameof(endTime));

        if (graceMinutes < 0 || graceMinutes > 240)
            throw new ArgumentException("Grace minutes must be between 0 and 240", nameof(graceMinutes));

        return new Shift
        {
            Name = name.Trim(),
            StartTime = startTime,
            EndTime = endTime,
            GraceMinutes = graceMinutes,
            Notes = notes?.Trim() ?? string.Empty
        };
    }

    public void Update(string name, TimeSpan startTime, TimeSpan endTime, int graceMinutes, string notes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time", nameof(endTime));

        if (graceMinutes < 0 || graceMinutes > 240)
            throw new ArgumentException("Grace minutes must be between 0 and 240", nameof(graceMinutes));

        Name = name.Trim();
        StartTime = startTime;
        EndTime = endTime;
        GraceMinutes = graceMinutes;
        Notes = notes?.Trim() ?? string.Empty;
    }
}


