namespace AutoPartShop.Domain.Entities;

/// <summary>
/// One employee's pay line inside a payroll run. Salary and attendance figures are
/// snapshotted at generation time so historic payslips stay stable when the employee
/// record changes later. Absence deduction: daily rate (salary / days in month) per
/// ABSENT day and half per HALF_DAY; LEAVE and HOLIDAY days are paid in v1.
/// </summary>
public class Payslip : AuditableEntity
{
    public Guid PayrollRunId { get; private set; }
    public Guid EmployeeId { get; private set; }

    // Employee snapshot
    public string EmployeeCode { get; private set; } = string.Empty;
    public string EmployeeName { get; private set; } = string.Empty;
    public string Designation { get; private set; } = string.Empty;
    public string Department { get; private set; } = string.Empty;
    public decimal MonthlySalary { get; private set; }

    // Attendance snapshot for the month
    public int DaysInMonth { get; private set; }
    public int PresentDays { get; private set; }
    public int LateDays { get; private set; }
    public int HalfDays { get; private set; }
    public int AbsentDays { get; private set; }
    public int LeaveDays { get; private set; }
    public int HolidayDays { get; private set; }

    // Manual adjustments (editable while the run is DRAFT)
    public decimal OvertimeAmount { get; private set; }
    public decimal BonusAmount { get; private set; }
    public decimal OtherAllowance { get; private set; }
    public decimal AdvanceDeduction { get; private set; }
    public decimal OtherDeduction { get; private set; }
    public string AdjustmentNotes { get; private set; } = string.Empty;

    // Computed
    public decimal AbsenceDeduction { get; private set; }
    public decimal GrossPay { get; private set; }
    public decimal TotalDeduction { get; private set; }
    public decimal NetPay { get; private set; }

    private Payslip() { }

    public static Payslip Create(Guid payrollRunId, Employee employee, int daysInMonth,
        int presentDays, int lateDays, int halfDays, int absentDays, int leaveDays, int holidayDays)
    {
        if (payrollRunId == Guid.Empty)
            throw new ArgumentException("PayrollRunId cannot be empty", nameof(payrollRunId));

        if (employee is null)
            throw new ArgumentNullException(nameof(employee));

        var payslip = new Payslip
        {
            PayrollRunId = payrollRunId,
            EmployeeId = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            EmployeeName = employee.Name,
            Designation = employee.Designation,
            Department = employee.Department,
            MonthlySalary = employee.MonthlySalary,
            DaysInMonth = daysInMonth,
            PresentDays = presentDays,
            LateDays = lateDays,
            HalfDays = halfDays,
            AbsentDays = absentDays,
            LeaveDays = leaveDays,
            HolidayDays = holidayDays
        };
        payslip.Recalculate();
        return payslip;
    }

    public void UpdateAdjustments(decimal overtimeAmount, decimal bonusAmount, decimal otherAllowance,
        decimal advanceDeduction, decimal otherDeduction, string adjustmentNotes)
    {
        if (overtimeAmount < 0 || bonusAmount < 0 || otherAllowance < 0 || advanceDeduction < 0 || otherDeduction < 0)
            throw new ArgumentException("Adjustment amounts cannot be negative");

        OvertimeAmount = overtimeAmount;
        BonusAmount = bonusAmount;
        OtherAllowance = otherAllowance;
        AdvanceDeduction = advanceDeduction;
        OtherDeduction = otherDeduction;
        AdjustmentNotes = adjustmentNotes?.Trim() ?? string.Empty;
        Recalculate();
    }

    private void Recalculate()
    {
        var dailyRate = DaysInMonth > 0 ? MonthlySalary / DaysInMonth : 0m;
        AbsenceDeduction = Math.Round(dailyRate * (AbsentDays + 0.5m * HalfDays), 2);

        GrossPay = Math.Round(MonthlySalary + OvertimeAmount + BonusAmount + OtherAllowance, 2);
        TotalDeduction = Math.Round(AbsenceDeduction + AdvanceDeduction + OtherDeduction, 2);
        NetPay = GrossPay - TotalDeduction;
    }
}
