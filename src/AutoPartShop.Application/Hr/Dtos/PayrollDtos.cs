namespace AutoPartShop.Application.Hr.Dtos
{
    public class GeneratePayrollRequest
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class UpdatePayslipRequest
    {
        public decimal OvertimeAmount { get; set; }
        public decimal BonusAmount { get; set; }
        public decimal OtherAllowance { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal AdvanceDeduction { get; set; }
        public decimal TaxDeduction { get; set; }
        public decimal OtherDeduction { get; set; }
        public string AdjustmentNotes { get; set; } = string.Empty;
    }

    public class SendPayslipsResponse
    {
        public int EmailsSent { get; set; }
        public int SmsSent { get; set; }
        public int Skipped { get; set; }
    }

    public class PayPayrollRequest
    {
        public string PaymentMethod { get; set; } = "CASH";  // CASH, BANK_TRANSFER, CHECK
    }

    public class PayrollRunResponse
    {
        public Guid Id { get; set; }
        public string RunCode { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public decimal TotalGross { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal TotalNet { get; set; }
        public int EmployeeCount { get; set; }
        public string ApprovedBy { get; set; } = string.Empty;
        public DateTime? ApprovedAt { get; set; }
        public string PaidBy { get; set; } = string.Empty;
        public DateTime? PaidAt { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public Guid? ExpenseId { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<PayslipResponse> Payslips { get; set; } = [];
    }

    public class PayslipResponse
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public decimal MonthlySalary { get; set; }
        public int DaysInMonth { get; set; }
        public int PresentDays { get; set; }
        public int LateDays { get; set; }
        public int HalfDays { get; set; }
        public int AbsentDays { get; set; }
        public int LeaveDays { get; set; }
        public int HolidayDays { get; set; }
        public decimal OvertimeAmount { get; set; }
        public decimal BonusAmount { get; set; }
        public decimal OtherAllowance { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal MonthlySalesTotal { get; set; }
        public decimal AdvanceDeduction { get; set; }
        public decimal TaxDeduction { get; set; }
        public decimal OtherDeduction { get; set; }
        public string AdjustmentNotes { get; set; } = string.Empty;
        public decimal AbsenceDeduction { get; set; }
        public decimal GrossPay { get; set; }
        public decimal TotalDeduction { get; set; }
        public decimal NetPay { get; set; }
    }
}
