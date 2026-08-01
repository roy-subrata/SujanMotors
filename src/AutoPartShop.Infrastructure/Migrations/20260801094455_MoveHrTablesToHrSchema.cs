using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveHrTablesToHrSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "hr");

            migrationBuilder.RenameTable(
                name: "Shifts",
                newName: "Shifts",
                newSchema: "hr");

            migrationBuilder.RenameTable(
                name: "SalaryAdvances",
                newName: "SalaryAdvances",
                newSchema: "hr");

            migrationBuilder.RenameTable(
                name: "Payslips",
                newName: "Payslips",
                newSchema: "hr");

            migrationBuilder.RenameTable(
                name: "PayrollRuns",
                newName: "PayrollRuns",
                newSchema: "hr");

            migrationBuilder.RenameTable(
                name: "LeaveRequests",
                newName: "LeaveRequests",
                newSchema: "hr");

            migrationBuilder.RenameTable(
                name: "Holidays",
                newName: "Holidays",
                newSchema: "hr");

            migrationBuilder.RenameTable(
                name: "Employees",
                newName: "Employees",
                newSchema: "hr");

            migrationBuilder.RenameTable(
                name: "AttendanceRecords",
                newName: "AttendanceRecords",
                newSchema: "hr");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Shifts",
                schema: "hr",
                newName: "Shifts");

            migrationBuilder.RenameTable(
                name: "SalaryAdvances",
                schema: "hr",
                newName: "SalaryAdvances");

            migrationBuilder.RenameTable(
                name: "Payslips",
                schema: "hr",
                newName: "Payslips");

            migrationBuilder.RenameTable(
                name: "PayrollRuns",
                schema: "hr",
                newName: "PayrollRuns");

            migrationBuilder.RenameTable(
                name: "LeaveRequests",
                schema: "hr",
                newName: "LeaveRequests");

            migrationBuilder.RenameTable(
                name: "Holidays",
                schema: "hr",
                newName: "Holidays");

            migrationBuilder.RenameTable(
                name: "Employees",
                schema: "hr",
                newName: "Employees");

            migrationBuilder.RenameTable(
                name: "AttendanceRecords",
                schema: "hr",
                newName: "AttendanceRecords");
        }
    }
}
