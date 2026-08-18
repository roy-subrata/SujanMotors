using AutoPartShop.Application.Common;
using AutoPartShop.Domain.Enums.HR;

namespace AutoPartShop.Application.HR.Dtos
{
    public class SalaryAdvanceQuery : BaseQuery
    {
        public SalaryAdvanceStatus? Status { get; set; }
        public Guid? EmployeeId { get; set; }
    }

    public class SalaryAdvanceResponse
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime AdvanceDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public SalaryAdvanceStatus Status { get; set; }
        public DateTime? SettledAt { get; set; }
        public string? SettledRunCode { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Body for raising an advance request. Approving it is a separate call.</summary>
    public class GiveAdvanceRequest
    {
        public Guid EmployeeId { get; set; }
        public DateTime AdvanceDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "CASH";
        public string Notes { get; set; } = string.Empty;
    }

    public class RejectAdvanceRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}
