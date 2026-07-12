namespace AutoPartShop.Application.Hr.Dtos
{
    /// <summary>One row of the daily attendance sheet: an active employee plus their (optional) mark for the day.</summary>
    public class DailyAttendanceRow
    {
        public Guid EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string? ShiftName { get; set; }
        public bool IsMarked { get; set; }
        public string Status { get; set; } = string.Empty;
        public TimeSpan? CheckInTime { get; set; }
        public TimeSpan? CheckOutTime { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class MarkAttendanceRequest
    {
        public DateTime Date { get; set; }
        public List<MarkAttendanceEntry> Entries { get; set; } = [];
    }

    public class MarkAttendanceEntry
    {
        public Guid EmployeeId { get; set; }
        public string Status { get; set; } = string.Empty;
        public TimeSpan? CheckInTime { get; set; }
        public TimeSpan? CheckOutTime { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class MonthlyAttendanceSummaryRow
    {
        public Guid EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public int PresentDays { get; set; }
        public int LateDays { get; set; }
        public int HalfDays { get; set; }
        public int AbsentDays { get; set; }
        public int LeaveDays { get; set; }
        public int HolidayDays { get; set; }
        public int MarkedDays { get; set; }
    }

    public class PunchRequest
    {
        public string EmployeeCode { get; set; } = string.Empty;
        public DateTime? Timestamp { get; set; }  // Device time; defaults to server local time
    }

    public class AttendanceRecordResponse
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan? CheckInTime { get; set; }
        public TimeSpan? CheckOutTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}
