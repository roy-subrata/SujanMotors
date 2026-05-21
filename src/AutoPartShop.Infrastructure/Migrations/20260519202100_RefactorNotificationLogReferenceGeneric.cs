using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorNotificationLogReferenceGeneric : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SalesOrderId",
                table: "NotificationLogs",
                newName: "ReferenceId");

            migrationBuilder.AddColumn<string>(
                name: "ReferenceType",
                table: "NotificationLogs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReferenceType",
                table: "NotificationLogs");

            migrationBuilder.RenameColumn(
                name: "ReferenceId",
                table: "NotificationLogs",
                newName: "SalesOrderId");
        }
    }
}
