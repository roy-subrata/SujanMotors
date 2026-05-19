using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultCurrencyIdSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Get BDT currency ID (base currency)
            var now = DateTime.UtcNow;
            var bdtId = migrationBuilder.Sql(
                @"SELECT TOP 1 Id FROM Currencies WHERE Code = 'BDT' AND Isdeleted = 0"
            );

            // Insert DEFAULT_CURRENCY_ID setting pointing to BDT
            migrationBuilder.Sql(
                @"
                DECLARE @bdtId UNIQUEIDENTIFIER
                SELECT TOP 1 @bdtId = Id FROM Currencies WHERE Code = 'BDT' AND Isdeleted = 0

                IF NOT EXISTS (SELECT 1 FROM ApplicationSettings WHERE [Key] = 'DEFAULT_CURRENCY_ID')
                BEGIN
                    INSERT INTO ApplicationSettings (Id, [Key], Value, DataType, Category, Description, IsSystemSetting, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, Isdeleted)
                    VALUES (
                        NEWID(),
                        'DEFAULT_CURRENCY_ID',
                        CAST(@bdtId AS NVARCHAR(50)),
                        'GUID',
                        'CURRENCY',
                        'Default currency ID for new transactions',
                        1,
                        GETUTCDATE(),
                        GETUTCDATE(),
                        'System',
                        'System',
                        0
                    )
                END
                "
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove DEFAULT_CURRENCY_ID setting
            migrationBuilder.Sql(
                @"DELETE FROM ApplicationSettings WHERE [Key] = 'DEFAULT_CURRENCY_ID'"
            );
        }
    }
}
