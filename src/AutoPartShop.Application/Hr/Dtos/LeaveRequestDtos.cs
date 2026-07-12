using AutoPartShop.Application.Common;

namespace AutoPartShop.Application.Hr.Dtos
{
    public class LeaveRequestQuery : BaseQuery
    {
        public string Status { get; set; } = "";
        public Guid? EmployeeId { get; set; }
    }

    public class LeaveRequestResponse
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string LeaveType { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalDays { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string DecisionBy { get; set; } = string.Empty;
        public DateTime? DecisionAt { get; set; }
        public string DecisionNotes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateLeaveRequestRequest
    {
        public Guid EmployeeId { get; set; }
        public string LeaveType { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class UpdateLeaveRequestRequest
    {
        public string LeaveType { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class LeaveDecisionRequest
    {
        public string Notes { get; set; } = string.Empty;
    }
}
