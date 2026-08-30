namespace AutoPartShop.Domain.Enums.HR;

/// <summary>Recovery status of a cash advance given against future salary.</summary>
/// <summary>
/// Stored as a string (see SalaryAdvanceConfiguration), so member order is not significant.
/// REQUESTED -> OUTSTANDING is the approval gate: no cash leaves the till until an advance is
/// approved, at which point the SALARY_ADVANCE expense is posted and payroll starts recovering it.
/// </summary>
public enum SalaryAdvanceStatus
{
    /// <summary>Raised by or for an employee, awaiting approval. No cash has moved.</summary>
    REQUESTED,

    /// <summary>Approved and paid out; being recovered from payroll.</summary>
    OUTSTANDING,

    /// <summary>Fully recovered.</summary>
    SETTLED,

    /// <summary>Declined before any cash moved.</summary>
    REJECTED
}
