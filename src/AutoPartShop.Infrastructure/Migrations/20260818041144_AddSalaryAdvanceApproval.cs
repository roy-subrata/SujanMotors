using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalaryAdvanceApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                schema: "hr",
                table: "SalaryAdvances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                schema: "hr",
                table: "SalaryAdvances",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                schema: "hr",
                table: "SalaryAdvances",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                schema: "hr",
                table: "SalaryAdvances");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                schema: "hr",
                table: "SalaryAdvances");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                schema: "hr",
                table: "SalaryAdvances");
        }
    }
}
