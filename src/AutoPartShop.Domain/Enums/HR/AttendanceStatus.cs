namespace AutoPartShop.Domain.Enums.HR;

/// <summary>Attendance outcome for an employee on a given calendar day.</summary>
public enum AttendanceStatus
{
    PRESENT,
    LATE,
    HALF_DAY,
    ABSENT,
    LEAVE,
    HOLIDAY
}
