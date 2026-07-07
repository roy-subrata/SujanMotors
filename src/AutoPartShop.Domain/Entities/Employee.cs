namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a shop staff member (HR master record).
/// Optionally linked to an ApplicationUser login account — not every employee
/// needs system access, and not every user (e.g. online customers) is an employee.
/// </summary>
public class Employee : AuditableEntity
{
    public string EmployeeCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string NidNumber { get; private set; } = string.Empty;  // National ID
    public DateTime? DateOfBirth { get; private set; }
    public string Gender { get; private set; } = string.Empty;  // MALE, FEMALE, OTHER
    public string Address { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;

    public string Designation { get; private set; } = string.Empty;  // e.g. Salesperson, Cashier, Storekeeper
    public string Department { get; private set; } = string.Empty;   // e.g. SALES, WAREHOUSE, ACCOUNTS, ADMIN
    public DateTime JoinDate { get; private set; }
    public DateTime? EndDate { get; private set; }  // Set when employment ends
    public string EmploymentType { get; private set; } = "FULL_TIME";  // FULL_TIME, PART_TIME, CONTRACT

    public decimal MonthlySalary { get; private set; }
    public string Currency { get; private set; } = "BDT";  // ISO 4217 currency code

    public string EmergencyContactName { get; private set; } = string.Empty;
    public string EmergencyContactPhone { get; private set; } = string.Empty;

    public string Status { get; private set; } = "ACTIVE";  // ACTIVE, INACTIVE
    public string Notes { get; private set; } = string.Empty;

    // Optional link to a login account (ApplicationUser lives in the Identity store)
    public Guid? UserId { get; private set; }

    private Employee() { }

    public static Employee Create(string employeeCode, string name, string phone,
        DateTime joinDate, string designation, string department,
        decimal monthlySalary, string employmentType = "FULL_TIME",
        string email = "", string nidNumber = "", DateTime? dateOfBirth = null,
        string gender = "", string address = "", string city = "",
        string emergencyContactName = "", string emergencyContactPhone = "",
        string notes = "", string currency = "BDT")
    {
        if (string.IsNullOrWhiteSpace(employeeCode))
            throw new ArgumentException("EmployeeCode cannot be empty", nameof(employeeCode));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone cannot be empty", nameof(phone));

        if (joinDate == default)
            throw new ArgumentException("JoinDate is required", nameof(joinDate));

        if (monthlySalary < 0)
            throw new ArgumentException("MonthlySalary cannot be negative", nameof(monthlySalary));

        return new Employee
        {
            EmployeeCode = employeeCode.Trim().ToUpper(),
            Name = name.Trim(),
            Phone = phone.Trim(),
            Email = email?.Trim() ?? string.Empty,
            NidNumber = nidNumber?.Trim() ?? string.Empty,
            DateOfBirth = dateOfBirth?.Date,
            Gender = gender?.Trim().ToUpper() ?? string.Empty,
            Address = address?.Trim() ?? string.Empty,
            City = city?.Trim() ?? string.Empty,
            Designation = designation?.Trim() ?? string.Empty,
            Department = department?.Trim().ToUpper() ?? string.Empty,
            JoinDate = joinDate.Date,
            EmploymentType = string.IsNullOrWhiteSpace(employmentType) ? "FULL_TIME" : employmentType.Trim().ToUpper(),
            MonthlySalary = monthlySalary,
            Currency = string.IsNullOrWhiteSpace(currency) ? "BDT" : currency.Trim().ToUpper(),
            EmergencyContactName = emergencyContactName?.Trim() ?? string.Empty,
            EmergencyContactPhone = emergencyContactPhone?.Trim() ?? string.Empty,
            Notes = notes?.Trim() ?? string.Empty,
            Status = "ACTIVE"
        };
    }

    public void UpdateInfo(string name, string phone, string email, string nidNumber,
        DateTime? dateOfBirth, string gender, string address, string city,
        string designation, string department, DateTime joinDate, string employmentType,
        decimal monthlySalary, string emergencyContactName, string emergencyContactPhone, string notes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone cannot be empty", nameof(phone));

        if (joinDate == default)
            throw new ArgumentException("JoinDate is required", nameof(joinDate));

        if (monthlySalary < 0)
            throw new ArgumentException("MonthlySalary cannot be negative", nameof(monthlySalary));

        Name = name.Trim();
        Phone = phone.Trim();
        Email = email?.Trim() ?? string.Empty;
        NidNumber = nidNumber?.Trim() ?? string.Empty;
        DateOfBirth = dateOfBirth?.Date;
        Gender = gender?.Trim().ToUpper() ?? string.Empty;
        Address = address?.Trim() ?? string.Empty;
        City = city?.Trim() ?? string.Empty;
        Designation = designation?.Trim() ?? string.Empty;
        Department = department?.Trim().ToUpper() ?? string.Empty;
        JoinDate = joinDate.Date;
        EmploymentType = string.IsNullOrWhiteSpace(employmentType) ? "FULL_TIME" : employmentType.Trim().ToUpper();
        MonthlySalary = monthlySalary;
        EmergencyContactName = emergencyContactName?.Trim() ?? string.Empty;
        EmergencyContactPhone = emergencyContactPhone?.Trim() ?? string.Empty;
        Notes = notes?.Trim() ?? string.Empty;
    }

    public void Activate()
    {
        Status = "ACTIVE";
        EndDate = null;
    }

    public void Deactivate(DateTime? endDate = null)
    {
        Status = "INACTIVE";
        EndDate = (endDate ?? DateTime.UtcNow).Date;
    }

    public void LinkUserAccount(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(userId));

        UserId = userId;
    }

    public void UnlinkUserAccount() => UserId = null;
}
