using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedSupplierPaymentConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayment_PaymentProvider_PaymentProviderId",
                table: "SupplierPayment");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayment_PurchaseOrder_PurchaseOrderId",
                table: "SupplierPayment");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayment_Suppliers_SupplierId",
                table: "SupplierPayment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SupplierPayment",
                table: "SupplierPayment");

            migrationBuilder.RenameTable(
                name: "SupplierPayment",
                newName: "SupplierPayments");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierPayment_SupplierId",
                table: "SupplierPayments",
                newName: "IX_SupplierPayments_SupplierId");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierPayment_PurchaseOrderId",
                table: "SupplierPayments",
                newName: "IX_SupplierPayments_PurchaseOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierPayment_PaymentProviderId",
                table: "SupplierPayments",
                newName: "IX_SupplierPayments_PaymentProviderId");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionNumber",
                table: "SupplierPayments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "SupplierPayments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "PENDING",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceNumber",
                table: "SupplierPayments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ProcessedBy",
                table: "SupplierPayments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentType",
                table: "SupplierPayments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "REGULAR",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "SupplierPayments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "SupplierPayments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<bool>(
                name: "IsReconciled",
                table: "SupplierPayments",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceNumber",
                table: "SupplierPayments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "SupplierPayments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "SupplierPayments",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "USD",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ConfirmedBy",
                table: "SupplierPayments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "AuthorizationCode",
                table: "SupplierPayments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SupplierPayments",
                table: "SupplierPayments",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_InvoiceNumber",
                table: "SupplierPayments",
                column: "InvoiceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_PaymentDate",
                table: "SupplierPayments",
                column: "PaymentDate");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_Status",
                table: "SupplierPayments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_TransactionNumber",
                table: "SupplierPayments",
                column: "TransactionNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_PaymentProvider_PaymentProviderId",
                table: "SupplierPayments",
                column: "PaymentProviderId",
                principalTable: "PaymentProvider",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_PurchaseOrder_PurchaseOrderId",
                table: "SupplierPayments",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrder",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_Suppliers_SupplierId",
                table: "SupplierPayments",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_PaymentProvider_PaymentProviderId",
                table: "SupplierPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_PurchaseOrder_PurchaseOrderId",
                table: "SupplierPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_Suppliers_SupplierId",
                table: "SupplierPayments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SupplierPayments",
                table: "SupplierPayments");

            migrationBuilder.DropIndex(
                name: "IX_SupplierPayments_InvoiceNumber",
                table: "SupplierPayments");

            migrationBuilder.DropIndex(
                name: "IX_SupplierPayments_PaymentDate",
                table: "SupplierPayments");

            migrationBuilder.DropIndex(
                name: "IX_SupplierPayments_Status",
                table: "SupplierPayments");

            migrationBuilder.DropIndex(
                name: "IX_SupplierPayments_TransactionNumber",
                table: "SupplierPayments");

            migrationBuilder.RenameTable(
                name: "SupplierPayments",
                newName: "SupplierPayment");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierPayments_SupplierId",
                table: "SupplierPayment",
                newName: "IX_SupplierPayment_SupplierId");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierPayments_PurchaseOrderId",
                table: "SupplierPayment",
                newName: "IX_SupplierPayment_PurchaseOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierPayments_PaymentProviderId",
                table: "SupplierPayment",
                newName: "IX_SupplierPayment_PaymentProviderId");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionNumber",
                table: "SupplierPayment",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "SupplierPayment",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "PENDING");

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceNumber",
                table: "SupplierPayment",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "ProcessedBy",
                table: "SupplierPayment",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentType",
                table: "SupplierPayment",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "REGULAR");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "SupplierPayment",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "SupplierPayment",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<bool>(
                name: "IsReconciled",
                table: "SupplierPayment",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceNumber",
                table: "SupplierPayment",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "SupplierPayment",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "SupplierPayment",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldDefaultValue: "USD");

            migrationBuilder.AlterColumn<string>(
                name: "ConfirmedBy",
                table: "SupplierPayment",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "AuthorizationCode",
                table: "SupplierPayment",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SupplierPayment",
                table: "SupplierPayment",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayment_PaymentProvider_PaymentProviderId",
                table: "SupplierPayment",
                column: "PaymentProviderId",
                principalTable: "PaymentProvider",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayment_PurchaseOrder_PurchaseOrderId",
                table: "SupplierPayment",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrder",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayment_Suppliers_SupplierId",
                table: "SupplierPayment",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
