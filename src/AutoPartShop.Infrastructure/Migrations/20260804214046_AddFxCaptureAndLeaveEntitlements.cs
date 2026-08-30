using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFxCaptureAndLeaveEntitlements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "SupplierPayments",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "BDT",
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldDefaultValue: "USD");

            migrationBuilder.AddColumn<decimal>(
                name: "BaseAmount",
                table: "SupplierPayments",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FxRateToBase",
                table: "SupplierPayments",
                type: "decimal(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseGrandTotal",
                table: "SalesOrders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FxRateToBase",
                table: "SalesOrders",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseTotalAmount",
                table: "PurchaseOrders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FxRateToBase",
                table: "PurchaseOrders",
                type: "decimal(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseGrandTotal",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FxRateToBase",
                table: "Invoices",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AnnualLeaveEntitlement",
                schema: "hr",
                table: "Employees",
                type: "decimal(10,1)",
                precision: 10,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CasualLeaveEntitlement",
                schema: "hr",
                table: "Employees",
                type: "decimal(10,1)",
                precision: 10,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SickLeaveEntitlement",
                schema: "hr",
                table: "Employees",
                type: "decimal(10,1)",
                precision: 10,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseAmount",
                table: "CustomerPayments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FxRateToBase",
                table: "CustomerPayments",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseAmount",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "FxRateToBase",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "BaseGrandTotal",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "FxRateToBase",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "BaseTotalAmount",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "FxRateToBase",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "BaseGrandTotal",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "FxRateToBase",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "AnnualLeaveEntitlement",
                schema: "hr",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CasualLeaveEntitlement",
                schema: "hr",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "SickLeaveEntitlement",
                schema: "hr",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "BaseAmount",
                table: "CustomerPayments");

            migrationBuilder.DropColumn(
                name: "FxRateToBase",
                table: "CustomerPayments");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "SupplierPayments",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "USD",
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldDefaultValue: "BDT");
        }
    }
}
