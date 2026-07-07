namespace AutoPartShop.Application.Hr.Dtos
{
    public class EmployeeResponse
    {
        public Guid Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NidNumber { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public DateTime JoinDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string EmploymentType { get; set; } = string.Empty;
        public decimal MonthlySalary { get; set; }
        public string Currency { get; set; } = string.Empty;
        public Guid? ShiftId { get; set; }
        public string? ShiftName { get; set; }
        public decimal MonthlyTaxDeduction { get; set; }
        public decimal CommissionRate { get; set; }
        public string EmergencyContactName { get; set; } = string.Empty;
        public string EmergencyContactPhone { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public Guid? UserId { get; set; }
        public string? UserName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
