namespace AutoPartShop.Domain.Entities;

/// <summary>
/// A leave application for an employee over an inclusive date range.
/// Entered by Admin/Manager on the employee's behalf in v1 (no self-service portal).
/// </summary>
public class LeaveRequest : AuditableEntity
{
    public Guid EmployeeId { get; private set; }
    public string LeaveType { get; private set; } = "CASUAL";  // CASUAL, SICK, ANNUAL, UNPAID
    public DateTime FromDate { get; private set; }
    public DateTime ToDate { get; private set; }
    public int TotalDays { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string Status { get; private set; } = "PENDING";  // PENDING, APPROVED, REJECTED, CANCELLED
    public string DecisionBy { get; private set; } = string.Empty;
    public DateTime? DecisionAt { get; private set; }
    public string DecisionNotes { get; private set; } = string.Empty;

    private static readonly string[] ValidLeaveTypes = ["CASUAL", "SICK", "ANNUAL", "UNPAID"];

    private LeaveRequest() { }

    public static LeaveRequest Create(Guid employeeId, string leaveType,
        DateTime fromDate, DateTime toDate, string reason = "")
    {
        if (employeeId == Guid.Empty)
            throw new ArgumentException("EmployeeId cannot be empty", nameof(employeeId));

        var normalized = leaveType?.Trim().ToUpper() ?? string.Empty;
        if (!ValidLeaveTypes.Contains(normalized))
            throw new ArgumentException($"Invalid leave type '{leaveType}'", nameof(leaveType));

        if (fromDate == default || toDate == default)
            throw new ArgumentException("From and To dates are required");

        if (toDate.Date < fromDate.Date)
            throw new ArgumentException("To date cannot be before From date", nameof(toDate));

        return new LeaveRequest
        {
            EmployeeId = employeeId,
            LeaveType = normalized,
            FromDate = fromDate.Date,
            ToDate = toDate.Date,
            TotalDays = (toDate.Date - fromDate.Date).Days + 1,
            Reason = reason?.Trim() ?? string.Empty,
            Status = "PENDING"
        };
    }

    public void Update(string leaveType, DateTime fromDate, DateTime toDate, string reason)
    {
        if (Status != "PENDING")
            throw new InvalidOperationException("Only pending leave requests can be edited");

        var normalized = leaveType?.Trim().ToUpper() ?? string.Empty;
        if (!ValidLeaveTypes.Contains(normalized))
            throw new ArgumentException($"Invalid leave type '{leaveType}'", nameof(leaveType));

        if (toDate.Date < fromDate.Date)
            throw new ArgumentException("To date cannot be before From date", nameof(toDate));

        LeaveType = normalized;
        FromDate = fromDate.Date;
        ToDate = toDate.Date;
        TotalDays = (toDate.Date - fromDate.Date).Days + 1;
        Reason = reason?.Trim() ?? string.Empty;
    }

    public void Approve(string decisionBy, string notes = "")
    {
        if (Status != "PENDING")
            throw new InvalidOperationException($"Cannot approve a {Status} leave request");

        Status = "APPROVED";
        DecisionBy = decisionBy?.Trim() ?? string.Empty;
        DecisionAt = DateTime.UtcNow;
        DecisionNotes = notes?.Trim() ?? string.Empty;
    }

    public void Reject(string decisionBy, string notes = "")
    {
        if (Status != "PENDING")
            throw new InvalidOperationException($"Cannot reject a {Status} leave request");

        Status = "REJECTED";
        DecisionBy = decisionBy?.Trim() ?? string.Empty;
        DecisionAt = DateTime.UtcNow;
        DecisionNotes = notes?.Trim() ?? string.Empty;
    }

    public void Cancel()
    {
        if (Status == "REJECTED" || Status == "CANCELLED")
            throw new InvalidOperationException($"Cannot cancel a {Status} leave request");

        Status = "CANCELLED";
    }
}
