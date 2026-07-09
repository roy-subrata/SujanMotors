using AutoPartShop.Domain.Entities;
using Xunit;

namespace AutoPartShop.Api.Tests;

public class HrFinanceSmokeTests
{
    // ─── Employee ─────────────────────────────────────────────────

    [Fact]
    public void Employee_Create_Valid_ShouldSetProperties()
    {
        var e = Employee.Create("EMP-001", "John Doe", "+8801711111111",
            new DateTime(2024, 1, 1), "Salesperson", "SALES", 30000m,
            "FULL_TIME", "john@shop.com", "1234567890", new DateTime(1990, 5, 15),
            "MALE", "123 Main St", "Dhaka", "Jane Doe", "+8801722222222",
            "Initial notes");
        Assert.Equal("EMP-001", e.EmployeeCode);
        Assert.Equal("John Doe", e.Name);
        Assert.Equal(30000m, e.MonthlySalary);
        Assert.Equal("SALES", e.Department);
        Assert.Equal("ACTIVE", e.Status);
        Assert.Equal("FULL_TIME", e.EmploymentType);
    }

    [Fact]
    public void Employee_Create_EmptyCode_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            Employee.Create("", "John", "123", DateTime.Today, "D", "DEPT", 10000m));

    [Fact]
    public void Employee_Create_EmptyName_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            Employee.Create("E001", "", "123", DateTime.Today, "D", "DEPT", 10000m));

    [Fact]
    public void Employee_Create_EmptyPhone_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            Employee.Create("E001", "John", "", DateTime.Today, "D", "DEPT", 10000m));

    [Fact]
    public void Employee_Create_DefaultJoinDate_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            Employee.Create("E001", "John", "123", default, "D", "DEPT", 10000m));

    [Fact]
    public void Employee_Create_NegativeSalary_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            Employee.Create("E001", "John", "123", DateTime.Today, "D", "DEPT", -1m));

    [Fact]
    public void Employee_ActivateDeactivate_ShouldToggle()
    {
        var e = CreateEmployee();
        e.Deactivate();
        Assert.Equal("INACTIVE", e.Status);
        Assert.NotNull(e.EndDate);
        e.Activate();
        Assert.Equal("ACTIVE", e.Status);
        Assert.Null(e.EndDate);
    }

    [Fact]
    public void Employee_UpdateInfo_ShouldModify()
    {
        var e = CreateEmployee();
        e.UpdateInfo("Jane Doe", "+8801733333333", "j@b.com", "NID999",
            new DateTime(1992, 3, 10), "FEMALE", "456 Oak St", "Chittagong",
            "Cashier", "ACCOUNTS", new DateTime(2024, 6, 1), "PART_TIME",
            25000m, "Emerg", "555", "Updated notes");
        Assert.Equal("Jane Doe", e.Name);
        Assert.Equal("+8801733333333", e.Phone);
        Assert.Equal("PART_TIME", e.EmploymentType);
        Assert.Equal(25000m, e.MonthlySalary);
    }

    [Fact]
    public void Employee_UpdateInfo_EmptyName_Throws()
    {
        var e = CreateEmployee();
        Assert.Throws<ArgumentException>(() =>
            e.UpdateInfo("", "123", "e", "n", null, "", "a", "c", "D", "DEPT",
                DateTime.Today, "FULL_TIME", 10000m, "", "", ""));
    }

    [Fact]
    public void Employee_UpdateCompensation_ShouldSet()
    {
        var e = CreateEmployee();
        var shiftId = Guid.NewGuid();
        e.UpdateCompensation(shiftId, 500m, 2.5m);
        Assert.Equal(shiftId, e.ShiftId);
        Assert.Equal(500m, e.MonthlyTaxDeduction);
        Assert.Equal(2.5m, e.CommissionRate);
    }

    [Fact]
    public void Employee_UpdateCompensation_NegativeTax_Throws()
    {
        var e = CreateEmployee();
        Assert.Throws<ArgumentException>(() => e.UpdateCompensation(null, -1m, 0));
    }

    [Fact]
    public void Employee_UpdateCompensation_InvalidCommission_Throws()
    {
        var e = CreateEmployee();
        Assert.Throws<ArgumentException>(() => e.UpdateCompensation(null, 0, 101m));
    }

    [Fact]
    public void Employee_LinkUserAccount_ShouldSet()
    {
        var e = CreateEmployee();
        var uid = Guid.NewGuid();
        e.LinkUserAccount(uid);
        Assert.Equal(uid, e.UserId);
        e.UnlinkUserAccount();
        Assert.Null(e.UserId);
    }

    [Fact]
    public void Employee_LinkUserAccount_EmptyId_Throws()
    {
        var e = CreateEmployee();
        Assert.Throws<ArgumentException>(() => e.LinkUserAccount(Guid.Empty));
    }

    private static Employee CreateEmployee() =>
        Employee.Create("E001", "John", "123", DateTime.Today, "Clerk", "ADMIN", 20000m);

    // ─── Shift ────────────────────────────────────────────────────

    [Fact]
    public void Shift_Create_Valid_ShouldSetProperties()
    {
        var s = Shift.Create("Morning", new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0), 15, "Standard shift");
        Assert.Equal("Morning", s.Name);
        Assert.Equal(9, s.StartTime.Hours);
        Assert.Equal(18, s.EndTime.Hours);
        Assert.Equal(15, s.GraceMinutes);
    }

    [Fact]
    public void Shift_Create_EmptyName_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            Shift.Create("", new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0)));

    [Fact]
    public void Shift_Create_EndBeforeStart_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            Shift.Create("Test", new TimeSpan(18, 0, 0), new TimeSpan(9, 0, 0)));

    [Fact]
    public void Shift_Create_NegativeGrace_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            Shift.Create("Test", new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0), -1));

    [Fact]
    public void Shift_Create_GraceOverMax_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            Shift.Create("Test", new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0), 241));

    [Fact]
    public void Shift_Update_ShouldModify()
    {
        var s = Shift.Create("Old", new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0));
        s.Update("New", new TimeSpan(10, 0, 0), new TimeSpan(19, 0, 0), 20, "Updated");
        Assert.Equal("New", s.Name);
        Assert.Equal(20, s.GraceMinutes);
    }

    // ─── Holiday ───────────────────────────────────────────────────

    [Fact]
    public void Holiday_Create_Valid_ShouldSetProperties()
    {
        var h = Holiday.Create(new DateTime(2025, 3, 26), "Independence Day");
        Assert.Equal(new DateTime(2025, 3, 26), h.Date);
        Assert.Equal("Independence Day", h.Name);
    }

    [Fact]
    public void Holiday_Create_DefaultDate_Throws() =>
        Assert.Throws<ArgumentException>(() => Holiday.Create(default, "Name"));

    [Fact]
    public void Holiday_Create_EmptyName_Throws() =>
        Assert.Throws<ArgumentException>(() => Holiday.Create(DateTime.Today, ""));

    [Fact]
    public void Holiday_Update_ShouldModify()
    {
        var h = Holiday.Create(new DateTime(2025, 3, 26), "Old");
        h.Update(new DateTime(2025, 4, 14), "Bengali New Year");
        Assert.Equal(new DateTime(2025, 4, 14), h.Date);
        Assert.Equal("Bengali New Year", h.Name);
    }

    // ─── AttendanceRecord ──────────────────────────────────────────

    [Fact]
    public void AttendanceRecord_Create_Valid_ShouldSetProperties()
    {
        var a = AttendanceRecord.Create(Guid.NewGuid(), DateTime.Today, "PRESENT",
            new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0));
        Assert.Equal("PRESENT", a.Status);
        Assert.Equal(new TimeSpan(9, 0, 0), a.CheckInTime);
        Assert.Equal(new TimeSpan(18, 0, 0), a.CheckOutTime);
    }

    [Fact]
    public void AttendanceRecord_Create_EmptyEmployeeId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            AttendanceRecord.Create(Guid.Empty, DateTime.Today, "PRESENT"));

    [Fact]
    public void AttendanceRecord_Create_DefaultDate_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            AttendanceRecord.Create(Guid.NewGuid(), default, "PRESENT"));

    [Fact]
    public void AttendanceRecord_Create_InvalidStatus_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            AttendanceRecord.Create(Guid.NewGuid(), DateTime.Today, "INVALID"));

    [Fact]
    public void AttendanceRecord_Create_CheckOutBeforeCheckIn_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            AttendanceRecord.Create(Guid.NewGuid(), DateTime.Today, "PRESENT",
                new TimeSpan(18, 0, 0), new TimeSpan(9, 0, 0)));

    [Fact]
    public void AttendanceRecord_Mark_ShouldChangeStatus()
    {
        var a = AttendanceRecord.Create(Guid.NewGuid(), DateTime.Today, "PRESENT");
        a.Mark("LATE", new TimeSpan(9, 30, 0), new TimeSpan(18, 0, 0), "Arrived late");
        Assert.Equal("LATE", a.Status);
        Assert.Equal(new TimeSpan(9, 30, 0), a.CheckInTime);
    }

    [Fact]
    public void AttendanceRecord_Mark_AllValidStatuses_ShouldWork()
    {
        var a = AttendanceRecord.Create(Guid.NewGuid(), DateTime.Today, "PRESENT");
        foreach (var s in new[] { "LATE", "HALF_DAY", "ABSENT", "LEAVE", "HOLIDAY", "PRESENT" })
        {
            a.Mark(s, null, null, "");
            Assert.Equal(s, a.Status);
        }
    }

    // ─── LeaveRequest ──────────────────────────────────────────────

    [Fact]
    public void LeaveRequest_Create_Valid_ShouldSetProperties()
    {
        var lr = LeaveRequest.Create(Guid.NewGuid(), "CASUAL",
            new DateTime(2025, 6, 1), new DateTime(2025, 6, 3), "Family event");
        Assert.Equal("CASUAL", lr.LeaveType);
        Assert.Equal(3, lr.TotalDays);
        Assert.Equal("PENDING", lr.Status);
    }

    [Fact]
    public void LeaveRequest_Create_EmptyEmployeeId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            LeaveRequest.Create(Guid.Empty, "CASUAL", DateTime.Today, DateTime.Today));

    [Fact]
    public void LeaveRequest_Create_InvalidLeaveType_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            LeaveRequest.Create(Guid.NewGuid(), "INVALID", DateTime.Today, DateTime.Today));

    [Fact]
    public void LeaveRequest_Create_ToBeforeFrom_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            LeaveRequest.Create(Guid.NewGuid(), "CASUAL",
                new DateTime(2025, 6, 5), new DateTime(2025, 6, 1)));

    [Fact]
    public void LeaveRequest_Approve_ShouldTransition()
    {
        var lr = LeaveRequest.Create(Guid.NewGuid(), "SICK",
            DateTime.Today, DateTime.Today, "Not feeling well");
        lr.Approve("Manager");
        Assert.Equal("APPROVED", lr.Status);
        Assert.Equal("Manager", lr.DecisionBy);
        Assert.NotNull(lr.DecisionAt);
    }

    [Fact]
    public void LeaveRequest_Approve_NonPending_Throws()
    {
        var lr = LeaveRequest.Create(Guid.NewGuid(), "SICK", DateTime.Today, DateTime.Today);
        lr.Approve("Mgr");
        Assert.Throws<InvalidOperationException>(() => lr.Approve("Mgr"));
    }

    [Fact]
    public void LeaveRequest_Reject_ShouldTransition()
    {
        var lr = LeaveRequest.Create(Guid.NewGuid(), "ANNUAL",
            DateTime.Today, DateTime.Today.AddDays(2));
        lr.Reject("Manager", "No available leave balance");
        Assert.Equal("REJECTED", lr.Status);
        Assert.Equal("Manager", lr.DecisionBy);
    }

    [Fact]
    public void LeaveRequest_Cancel_ShouldTransition()
    {
        var lr = LeaveRequest.Create(Guid.NewGuid(), "CASUAL", DateTime.Today, DateTime.Today);
        lr.Cancel();
        Assert.Equal("CANCELLED", lr.Status);
    }

    [Fact]
    public void LeaveRequest_Cancel_Rejected_Throws()
    {
        var lr = LeaveRequest.Create(Guid.NewGuid(), "CASUAL", DateTime.Today, DateTime.Today);
        lr.Reject("Mgr", "");
        Assert.Throws<InvalidOperationException>(() => lr.Cancel());
    }

    [Fact]
    public void LeaveRequest_Update_ShouldModify()
    {
        var lr = LeaveRequest.Create(Guid.NewGuid(), "CASUAL",
            new DateTime(2025, 6, 1), new DateTime(2025, 6, 3));
        lr.Update("SICK", new DateTime(2025, 7, 1), new DateTime(2025, 7, 2), "Fever");
        Assert.Equal("SICK", lr.LeaveType);
        Assert.Equal(2, lr.TotalDays);
    }

    [Fact]
    public void LeaveRequest_Update_NonPending_Throws()
    {
        var lr = LeaveRequest.Create(Guid.NewGuid(), "CASUAL", DateTime.Today, DateTime.Today);
        lr.Approve("Mgr");
        Assert.Throws<InvalidOperationException>(() =>
            lr.Update("SICK", DateTime.Today, DateTime.Today, ""));
    }

    // ─── SalaryAdvance ────────────────────────────────────────────

    [Fact]
    public void SalaryAdvance_Create_Valid_ShouldSetProperties()
    {
        var sa = SalaryAdvance.Create(Guid.NewGuid(), DateTime.Today, 5000m, "CASH", "Urgent need");
        Assert.Equal(5000m, sa.Amount);
        Assert.Equal("OUTSTANDING", sa.Status);
        Assert.Equal("CASH", sa.PaymentMethod);
    }

    [Fact]
    public void SalaryAdvance_Create_EmptyEmployeeId_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            SalaryAdvance.Create(Guid.Empty, DateTime.Today, 1000m));

    [Fact]
    public void SalaryAdvance_Create_DefaultDate_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            SalaryAdvance.Create(Guid.NewGuid(), default, 1000m));

    [Fact]
    public void SalaryAdvance_Create_ZeroAmount_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            SalaryAdvance.Create(Guid.NewGuid(), DateTime.Today, 0));

    [Fact]
    public void SalaryAdvance_Create_NegativeAmount_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            SalaryAdvance.Create(Guid.NewGuid(), DateTime.Today, -100m));

    [Fact]
    public void SalaryAdvance_Create_EmptyPaymentMethod_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            SalaryAdvance.Create(Guid.NewGuid(), DateTime.Today, 1000m, ""));

    [Fact]
    public void SalaryAdvance_LinkExpense_Settle_ShouldWork()
    {
        var sa = SalaryAdvance.Create(Guid.NewGuid(), DateTime.Today, 5000m);
        var expenseId = Guid.NewGuid();
        sa.LinkExpense(expenseId);
        Assert.Equal(expenseId, sa.ExpenseId);

        var payrollRunId = Guid.NewGuid();
        sa.Settle(payrollRunId);
        Assert.Equal("SETTLED", sa.Status);
        Assert.Equal(payrollRunId, sa.SettledPayrollRunId);
        Assert.NotNull(sa.SettledAt);
    }

    [Fact]
    public void SalaryAdvance_Settle_AlreadySettled_Throws()
    {
        var sa = SalaryAdvance.Create(Guid.NewGuid(), DateTime.Today, 5000m);
        sa.Settle(Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(() => sa.Settle(Guid.NewGuid()));
    }

    // ─── DailyExpense ─────────────────────────────────────────────

    [Fact]
    public void DailyExpense_Create_Valid_ShouldSetProperties()
    {
        var de = DailyExpense.Create(DateTime.Today, "RENT", 50000m,
            "Monthly rent", "BANK_TRANSFER", "Landlord", "BDT");
        Assert.Equal(DateTime.Today, de.ExpenseDate);
        Assert.Equal("RENT", de.Category);
        Assert.Equal(50000m, de.Amount);
        Assert.Equal("BANK_TRANSFER", de.PaymentMethod);
    }

    [Fact]
    public void DailyExpense_Create_ZeroAmount_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            DailyExpense.Create(DateTime.Today, "RENT", 0, "desc", "CASH"));

    [Fact]
    public void DailyExpense_Create_NegativeAmount_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            DailyExpense.Create(DateTime.Today, "RENT", -1, "desc", "CASH"));

    [Fact]
    public void DailyExpense_Create_EmptyCategory_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            DailyExpense.Create(DateTime.Today, "", 100, "desc", "CASH"));

    [Fact]
    public void DailyExpense_Create_EmptyDescription_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            DailyExpense.Create(DateTime.Today, "RENT", 100, "", "CASH"));

    [Fact]
    public void DailyExpense_Create_EmptyPaymentMethod_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            DailyExpense.Create(DateTime.Today, "RENT", 100, "desc", ""));

    [Fact]
    public void DailyExpense_Update_ShouldModify()
    {
        var de = DailyExpense.Create(DateTime.Today, "RENT", 50000m, "desc", "CASH");
        de.Update(DateTime.Today.AddDays(1), "UTILITIES", 3000m, "Electric bill", "BANK_TRANSFER", "DESCO");
        Assert.Equal("UTILITIES", de.Category);
        Assert.Equal(3000m, de.Amount);
        Assert.Equal("BANK_TRANSFER", de.PaymentMethod);
    }

    [Fact]
    public void DailyExpense_SetReference_UpdateNotes_ShouldWork()
    {
        var de = DailyExpense.Create(DateTime.Today, "RENT", 1000m, "desc", "CASH");
        de.SetReferenceNumber("CHK-001");
        Assert.Equal("CHK-001", de.ReferenceNumber);
        de.UpdateNotes("Paid on time");
        Assert.Equal("Paid on time", de.Notes);
    }

    [Fact]
    public void DailyExpense_SetRecurring_ShouldSet()
    {
        var de = DailyExpense.Create(DateTime.Today, "RENT", 1000m, "desc", "CASH");
        de.SetRecurring(true, "MONTHLY");
        Assert.True(de.IsRecurring);
        Assert.Equal("MONTHLY", de.RecurrencePattern);
        de.SetRecurring(false);
        Assert.False(de.IsRecurring);
        Assert.Equal("", de.RecurrencePattern);
    }

    [Fact]
    public void DailyExpense_AttachDocument_ShouldSet()
    {
        var de = DailyExpense.Create(DateTime.Today, "RENT", 1000m, "desc", "CASH");
        var id = Guid.NewGuid();
        de.AttachDocument(id);
        Assert.Equal(id, de.AttachmentId);
    }

    // ─── PayrollRun ───────────────────────────────────────────────

    [Fact]
    public void PayrollRun_Create_Valid_ShouldSetProperties()
    {
        var pr = PayrollRun.Create("PR-2025-06", 2025, 6, "June payroll");
        Assert.Equal("PR-2025-06", pr.RunCode);
        Assert.Equal(2025, pr.Year);
        Assert.Equal(6, pr.Month);
        Assert.Equal("DRAFT", pr.Status);
    }

    [Fact]
    public void PayrollRun_Create_EmptyRunCode_Throws() =>
        Assert.Throws<ArgumentException>(() => PayrollRun.Create("", 2025, 6));

    [Fact]
    public void PayrollRun_Create_InvalidYear_Throws() =>
        Assert.Throws<ArgumentException>(() => PayrollRun.Create("PR-001", 1999, 6));

    [Fact]
    public void PayrollRun_Create_InvalidMonth_Throws() =>
        Assert.Throws<ArgumentException>(() => PayrollRun.Create("PR-001", 2025, 0));

    [Fact]
    public void PayrollRun_EnsureDraft_Draft_DoesNotThrow()
    {
        var pr = PayrollRun.Create("PR-001", 2025, 6);
        pr.EnsureDraft();
    }

    [Fact]
    public void PayrollRun_EnsureDraft_NonDraft_Throws()
    {
        var pr = PayrollRun.Create("PR-001", 2025, 6);
        var emp = CreateEmployeeFull();
        var payslip = Payslip.Create(pr.Id, emp, 30, 25, 1, 1, 2, 1, 0);
        pr.Payslips.Add(payslip);
        pr.Approve("Manager");
        Assert.Throws<InvalidOperationException>(() => pr.EnsureDraft());
    }

    [Fact]
    public void PayrollRun_Approve_ShouldTransition()
    {
        var pr = PayrollRun.Create("PR-001", 2025, 6);
        var emp = CreateEmployeeFull();
        var payslip = Payslip.Create(pr.Id, emp, 30, 25, 1, 1, 2, 1, 0);
        pr.Payslips.Add(payslip);
        pr.Approve("Manager");
        Assert.Equal("APPROVED", pr.Status);
        Assert.Equal("Manager", pr.ApprovedBy);
        Assert.NotNull(pr.ApprovedAt);
    }

    [Fact]
    public void PayrollRun_Approve_NoPayslips_Throws()
    {
        var pr = PayrollRun.Create("PR-001", 2025, 6);
        Assert.Throws<InvalidOperationException>(() => pr.Approve("Manager"));
    }

    [Fact]
    public void PayrollRun_Approve_NonDraft_Throws()
    {
        var pr = PayrollRun.Create("PR-001", 2025, 6);
        var emp = CreateEmployeeFull();
        var ps = Payslip.Create(pr.Id, emp, 30, 25, 0, 0, 0, 5, 0);
        pr.Payslips.Add(ps);
        pr.Approve("Mgr");
        Assert.Throws<InvalidOperationException>(() => pr.Approve("Mgr"));
    }

    [Fact]
    public void PayrollRun_MarkPaid_ShouldTransition()
    {
        var pr = PayrollRun.Create("PR-001", 2025, 6);
        var emp = CreateEmployeeFull();
        var ps = Payslip.Create(pr.Id, emp, 30, 25, 1, 1, 2, 1, 0);
        pr.Payslips.Add(ps);
        pr.Approve("Manager");
        pr.MarkPaid("Cashier", "BANK_TRANSFER", Guid.NewGuid());
        Assert.Equal("PAID", pr.Status);
        Assert.Equal("BANK_TRANSFER", pr.PaymentMethod);
        Assert.NotNull(pr.PaidAt);
    }

    [Fact]
    public void PayrollRun_MarkPaid_NonApproved_Throws()
    {
        var pr = PayrollRun.Create("PR-001", 2025, 6);
        Assert.Throws<InvalidOperationException>(() =>
            pr.MarkPaid("Cashier", "CASH", Guid.NewGuid()));
    }

    [Fact]
    public void PayrollRun_MarkPaid_EmptyPaymentMethod_Throws()
    {
        var pr = PayrollRun.Create("PR-001", 2025, 6);
        var emp = CreateEmployeeFull();
        var ps = Payslip.Create(pr.Id, emp, 30, 25, 0, 0, 0, 5, 0);
        pr.Payslips.Add(ps);
        pr.Approve("Mgr");
        Assert.Throws<ArgumentException>(() =>
            pr.MarkPaid("Cashier", "", Guid.NewGuid()));
    }

    [Fact]
    public void PayrollRun_RecalculateTotals_ShouldSum()
    {
        var pr = PayrollRun.Create("PR-001", 2025, 6);
        var emp = CreateEmployeeFull();
        var ps1 = Payslip.Create(pr.Id, emp, 30, 25, 0, 0, 0, 5, 0);
        var ps2 = Payslip.Create(pr.Id, emp, 30, 30, 0, 0, 0, 0, 0);
        pr.Payslips.Add(ps1);
        pr.Payslips.Add(ps2);
        pr.RecalculateTotals();
        Assert.Equal(2, pr.EmployeeCount);
        Assert.True(pr.TotalGross > 0);
        Assert.True(pr.TotalNet > 0);
    }

    [Fact]
    public void PayrollRun_UpdateNotes_ShouldSet()
    {
        var pr = PayrollRun.Create("PR-001", 2025, 6);
        pr.UpdateNotes("Updated");
        Assert.Equal("Updated", pr.Notes);
        pr.UpdateNotes("");
        Assert.Equal("", pr.Notes);
    }

    // ─── Payslip ──────────────────────────────────────────────────

    [Fact]
    public void Payslip_Create_Valid_ShouldSetProperties()
    {
        var emp = CreateEmployeeFull();
        var ps = Payslip.Create(Guid.NewGuid(), emp, 30, 25, 2, 1, 1, 1, 0);
        Assert.Equal(emp.EmployeeCode, ps.EmployeeCode);
        Assert.Equal(emp.Name, ps.EmployeeName);
        Assert.Equal(30, ps.DaysInMonth);
        Assert.Equal(25, ps.PresentDays);
        Assert.Equal(1, ps.AbsentDays);
    }

    [Fact]
    public void Payslip_Create_EmptyPayrollRunId_Throws()
    {
        var emp = CreateEmployeeFull();
        Assert.Throws<ArgumentException>(() =>
            Payslip.Create(Guid.Empty, emp, 30, 25, 0, 0, 0, 0, 0));
    }

    [Fact]
    public void Payslip_Create_NullEmployee_Throws() =>
        Assert.Throws<ArgumentNullException>(() =>
            Payslip.Create(Guid.NewGuid(), null!, 30, 25, 0, 0, 0, 0, 0));

    [Fact]
    public void Payslip_ApplyGeneratedFigures_ShouldSet()
    {
        var emp = CreateEmployeeFull();
        var ps = Payslip.Create(Guid.NewGuid(), emp, 30, 25, 0, 0, 0, 5, 0);
        ps.ApplyGeneratedFigures(1000m, 500m, 100000m, 2m);
        Assert.Equal(1000m, ps.AdvanceDeduction);
        Assert.Equal(500m, ps.TaxDeduction);
        Assert.Equal(2000m, ps.CommissionAmount);
        Assert.Equal(100000m, ps.MonthlySalesTotal);
    }

    [Fact]
    public void Payslip_UpdateAdjustments_ShouldSet()
    {
        var emp = CreateEmployeeFull();
        var ps = Payslip.Create(Guid.NewGuid(), emp, 30, 25, 0, 0, 0, 5, 0);
        ps.UpdateAdjustments(500m, 1000m, 300m, 200m, 1500m, 600m, 100m, "Manual adj");
        Assert.Equal(500m, ps.OvertimeAmount);
        Assert.Equal(1000m, ps.BonusAmount);
        Assert.Equal(100m, ps.OtherDeduction);
    }

    [Fact]
    public void Payslip_UpdateAdjustments_Negative_Throws()
    {
        var emp = CreateEmployeeFull();
        var ps = Payslip.Create(Guid.NewGuid(), emp, 30, 25, 0, 0, 0, 5, 0);
        Assert.Throws<ArgumentException>(() =>
            ps.UpdateAdjustments(-1, 0, 0, 0, 0, 0, 0, ""));
    }

    [Fact]
    public void Payslip_ComputedFields_ShouldCalculate()
    {
        var emp = CreateEmployeeFull();
        var ps = Payslip.Create(Guid.NewGuid(), emp, 30, 25, 0, 0, 0, 5, 0);
        // MonthlySalary = 30000, DaysInMonth = 30 → daily rate = 1000
        // AbsentDays = 0 so no absence deduction in this test
        // Actually let's test with some absent days:
        var ps2 = Payslip.Create(Guid.NewGuid(), emp, 30, 25, 1, 2, 1, 1, 0);
        // daily rate = 30000/30 = 1000
        // absence = 1000 * (1 + 0.5 * 2) = 1000 * 2 = 2000
        // GrossPay = 30000 + 0 + 0 + 0 + 0 = 30000
        // TotalDeduction = 2000 + 0 + 0 + 0 = 2000
        // NetPay = 30000 - 2000 = 28000
        Assert.Equal(30000m, ps2.GrossPay);
        Assert.Equal(2000m, ps2.AbsenceDeduction);
        Assert.Equal(28000m, ps2.NetPay);
    }

    private static Employee CreateEmployeeFull() =>
        Employee.Create("EMP-001", "John Doe", "+8801711111111",
            new DateTime(2024, 1, 1), "Salesperson", "SALES", 30000m);

    // ─── Discount ─────────────────────────────────────────────────

    [Fact]
    public void Discount_Create_Valid_ShouldSetProperties()
    {
        var d = Discount.Create("Summer Sale", "PERCENTAGE", 10m, DateTime.Today);
        Assert.Equal("Summer Sale", d.Name);
        Assert.Equal("PERCENTAGE", d.Type);
        Assert.Equal(10m, d.Value);
        Assert.True(d.IsActive);
        Assert.Equal("CART", d.Scope);
        Assert.True(d.IsCartLevel);
    }

    [Fact]
    public void Discount_Create_EmptyName_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            Discount.Create("", "PERCENTAGE", 10m, DateTime.Today));

    [Fact]
    public void Discount_Create_InvalidType_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            Discount.Create("Sale", "INVALID", 10m, DateTime.Today));

    [Fact]
    public void Discount_Create_ZeroValue_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            Discount.Create("Sale", "PERCENTAGE", 0, DateTime.Today));

    [Fact]
    public void Discount_Create_PercentageOver100_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            Discount.Create("Sale", "PERCENTAGE", 101m, DateTime.Today));

    [Fact]
    public void Discount_Create_EndBeforeStart_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            Discount.Create("Sale", "FIXED", 50m, DateTime.Today, endDate: DateTime.Today.AddDays(-1)));

    [Fact]
    public void Discount_Create_VariantWithoutPart_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            Discount.Create("Sale", "FIXED", 50m, DateTime.Today, productVariantId: Guid.NewGuid()));

    [Fact]
    public void Discount_Scope_ProductLevel_ShouldWork()
    {
        var d = Discount.Create("Product Sale", "FIXED", 100m, DateTime.Today, partId: Guid.NewGuid());
        Assert.True(d.IsProductLevel);
        Assert.False(d.IsCartLevel);
        Assert.False(d.IsVariantLevel);
        Assert.Equal("PRODUCT", d.Scope);
    }

    [Fact]
    public void Discount_Scope_VariantLevel_ShouldWork()
    {
        var partId = Guid.NewGuid();
        var d = Discount.Create("Variant Sale", "PERCENTAGE", 15m, DateTime.Today,
            partId: partId, productVariantId: Guid.NewGuid());
        Assert.True(d.IsVariantLevel);
        Assert.Equal("VARIANT", d.Scope);
    }

    [Fact]
    public void Discount_CalculateDiscountAmount_Percentage_ShouldCalculate()
    {
        var d = Discount.Create("10% Off", "PERCENTAGE", 10m, DateTime.Today);
        Assert.Equal(50m, d.CalculateDiscountAmount(500m));
    }

    [Fact]
    public void Discount_CalculateDiscountAmount_Fixed_ShouldCalculate()
    {
        var d = Discount.Create("Fixed Off", "FIXED", 200m, DateTime.Today);
        Assert.Equal(200m, d.CalculateDiscountAmount(500m));
    }

    [Fact]
    public void Discount_CalculateDiscountAmount_FixedCappedAtPrice_ShouldCap()
    {
        var d = Discount.Create("Fixed Off", "FIXED", 200m, DateTime.Today);
        Assert.Equal(50m, d.CalculateDiscountAmount(50m));
    }

    [Fact]
    public void Discount_CalculateDiscountAmount_NonPositivePrice_ReturnsZero()
    {
        var d = Discount.Create("Sale", "FIXED", 100m, DateTime.Today);
        Assert.Equal(0, d.CalculateDiscountAmount(0));
        Assert.Equal(0, d.CalculateDiscountAmount(-10m));
    }

    [Fact]
    public void Discount_IsValidOn_ShouldCheckDate()
    {
        var d = Discount.Create("Sale", "FIXED", 100m, DateTime.Today.AddDays(-10),
            endDate: DateTime.Today.AddDays(10));
        Assert.True(d.IsValidOn(DateTime.Today));
        Assert.True(d.IsValidOn(DateTime.Today.AddDays(-10)));
        Assert.True(d.IsValidOn(DateTime.Today.AddDays(10)));
        Assert.False(d.IsValidOn(DateTime.Today.AddDays(-11)));
        Assert.False(d.IsValidOn(DateTime.Today.AddDays(11)));
    }

    [Fact]
    public void Discount_IsValidOn_Inactive_ReturnsFalse()
    {
        var d = Discount.Create("Sale", "FIXED", 100m, DateTime.Today.AddDays(-10),
            endDate: DateTime.Today.AddDays(10));
        d.Deactivate();
        Assert.False(d.IsValidOn(DateTime.Today));
    }

    [Fact]
    public void Discount_ActivateDeactivate_ShouldToggle()
    {
        var d = Discount.Create("Sale", "FIXED", 100m, DateTime.Today);
        d.Deactivate();
        Assert.False(d.IsActive);
        d.Activate();
        Assert.True(d.IsActive);
    }

    [Fact]
    public void Discount_Update_ShouldModify()
    {
        var d = Discount.Create("Old", "PERCENTAGE", 10m, DateTime.Today);
        d.Update("New", "FIXED", 200m, DateTime.Today.AddDays(1), false,
            DateTime.Today.AddMonths(1), "desc", "PROMO50", 500m);
        Assert.Equal("New", d.Name);
        Assert.Equal("FIXED", d.Type);
        Assert.Equal(200m, d.Value);
        Assert.False(d.IsActive);
        Assert.Equal("PROMO50", d.PromoCode);
        Assert.Equal(500m, d.MinimumCartAmount);
    }

    [Fact]
    public void Discount_Update_Validation_ShouldThrow()
    {
        var d = Discount.Create("Sale", "FIXED", 100m, DateTime.Today);
        Assert.Throws<ArgumentException>(() =>
            d.Update("", "FIXED", 100m, DateTime.Today, true));
        Assert.Throws<ArgumentException>(() =>
            d.Update("Sale", "INVALID", 100m, DateTime.Today, true));
        Assert.Throws<ArgumentException>(() =>
            d.Update("Sale", "FIXED", 0, DateTime.Today, true));
    }

    [Fact]
    public void Discount_Create_WithPromoCode_ShouldSet()
    {
        var d = Discount.Create("Promo", "FIXED", 50m, DateTime.Today,
            promoCode: "SAVE50", minimumCartAmount: 1000m);
        Assert.Equal("SAVE50", d.PromoCode);
        Assert.Equal(1000m, d.MinimumCartAmount);
    }
}
