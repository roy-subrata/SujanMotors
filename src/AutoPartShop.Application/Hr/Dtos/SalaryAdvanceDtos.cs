using AutoPartShop.Application.Common;

namespace AutoPartShop.Application.Hr.Dtos
{
    public class SalaryAdvanceQuery : BaseQuery
    {
        public string Status { get; set; } = "";
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
        public string Status { get; set; } = string.Empty;
        public DateTime? SettledAt { get; set; }
        public string? SettledRunCode { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class GiveAdvanceRequest
    {
        public Guid EmployeeId { get; set; }
        public DateTime AdvanceDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "CASH";
        public string Notes { get; set; } = string.Empty;
    }
}
