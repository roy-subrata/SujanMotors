using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiCurrencySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "SalesOrders",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "BDT");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "PurchaseOrders",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "BDT");

            migrationBuilder.AddColumn<string>(
                name: "CostPriceCurrency",
                table: "Parts",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "BDT");

            migrationBuilder.AddColumn<string>(
                name: "SellingPriceCurrency",
                table: "Parts",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "BDT");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Invoices",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "BDT");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "DailyExpenses",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "BDT");

            migrationBuilder.CreateTable(
                name: "ApplicationSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "STRING"),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "GENERAL"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsSystemSetting = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Isdeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DecimalPlaces = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsBaseCurrency = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Isdeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromCurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToCurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "MANUAL"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Isdeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExchangeRates_Currencies_FromCurrencyId",
                        column: x => x.FromCurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExchangeRates_Currencies_ToCurrencyId",
                        column: x => x.ToCurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationSettings_Category",
                table: "ApplicationSettings",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationSettings_IsSystemSetting",
                table: "ApplicationSettings",
                column: "IsSystemSetting");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationSettings_Key",
                table: "ApplicationSettings",
                column: "Key",
                unique: true,
                filter: "[Isdeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_Code",
                table: "Currencies",
                column: "Code",
                unique: true,
                filter: "[Isdeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_DisplayOrder",
                table: "Currencies",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_IsActive",
                table: "Currencies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_IsBaseCurrency",
                table: "Currencies",
                column: "IsBaseCurrency",
                filter: "[IsBaseCurrency] = 1 AND [Isdeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_EffectiveDate",
                table: "ExchangeRates",
                column: "EffectiveDate");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_ExpiryDate",
                table: "ExchangeRates",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_FromCurrencyId_ToCurrencyId_EffectiveDate",
                table: "ExchangeRates",
                columns: new[] { "FromCurrencyId", "ToCurrencyId", "EffectiveDate" },
                filter: "[Isdeleted] = 0 AND [IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_IsActive",
                table: "ExchangeRates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_ToCurrencyId",
                table: "ExchangeRates",
                column: "ToCurrencyId");

            // Seed Currencies
            var now = DateTime.UtcNow;
            var bdtId = Guid.NewGuid();
            var usdId = Guid.NewGuid();
            var inrId = Guid.NewGuid();
            var nprId = Guid.NewGuid();

            migrationBuilder.InsertData(
                table: "Currencies",
                columns: new[] { "Id", "Code", "Name", "Symbol", "DecimalPlaces", "IsActive", "IsBaseCurrency", "DisplayOrder", "CreatedDate", "ModifiedDate", "CreatedBy", "ModifiedBy", "Isdeleted" },
                values: new object[,]
                {
                    { bdtId, "BDT", "Bangladeshi Taka", "৳", 2, true, true, 1, now, now, "System", "System", false },
                    { usdId, "USD", "US Dollar", "$", 2, true, false, 2, now, now, "System", "System", false },
                    { inrId, "INR", "Indian Rupee", "₹", 2, true, false, 3, now, now, "System", "System", false },
                    { nprId, "NPR", "Nepalese Rupee", "रू", 2, true, false, 4, now, now, "System", "System", false }
                });

            // Seed Exchange Rates (Sample rates - Admin can update these)
            migrationBuilder.InsertData(
                table: "ExchangeRates",
                columns: new[] { "Id", "FromCurrencyId", "ToCurrencyId", "Rate", "EffectiveDate", "ExpiryDate", "Source", "IsActive", "Notes", "CreatedDate", "ModifiedDate", "CreatedBy", "ModifiedBy", "Isdeleted" },
                values: new object[,]
                {
                    // USD to BDT
                    { Guid.NewGuid(), usdId, bdtId, 110.00m, new DateTime(2024, 1, 1), null, "MANUAL", true, "Initial sample rate - please update with current rate", now, now, "System", "System", false },
                    // INR to BDT
                    { Guid.NewGuid(), inrId, bdtId, 1.30m, new DateTime(2024, 1, 1), null, "MANUAL", true, "Initial sample rate - please update with current rate", now, now, "System", "System", false },
                    // NPR to BDT
                    { Guid.NewGuid(), nprId, bdtId, 0.82m, new DateTime(2024, 1, 1), null, "MANUAL", true, "Initial sample rate - please update with current rate", now, now, "System", "System", false }
                });

            // Seed Application Settings
            migrationBuilder.InsertData(
                table: "ApplicationSettings",
                columns: new[] { "Id", "Key", "Value", "DataType", "Category", "Description", "IsSystemSetting", "CreatedDate", "ModifiedDate", "CreatedBy", "ModifiedBy", "Isdeleted" },
                values: new object[]
                {
                    Guid.NewGuid(), "BASE_CURRENCY", "BDT", "STRING", "CURRENCY", "Base currency code for the system (ISO 4217)", true, now, now, "System", "System", false
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationSettings");

            migrationBuilder.DropTable(
                name: "ExchangeRates");

            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CostPriceCurrency",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "SellingPriceCurrency",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "DailyExpenses");
        }
    }
}
