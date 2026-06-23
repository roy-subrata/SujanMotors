using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductEmbedding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The Embedding column uses the SQL Server 2025 native 'vector' type (ProductMajorVersion 17).
            // Older servers (2019 = 15, 2022 = 16) and managed hosts without vector support would fail
            // this CREATE TABLE on startup. Semantic search is a runtime-optional feature
            // (Embedding:BaseUrl blank → keyword fallback; the table is never read or written when
            // disabled), so we only create the table where the vector type exists. Done as raw SQL
            // because the typed column would otherwise emit 'vector(1536)' unconditionally.
            migrationBuilder.Sql(@"
IF CAST(SERVERPROPERTY('ProductMajorVersion') AS int) >= 17
BEGIN
    EXEC(N'
    CREATE TABLE [ProductEmbeddings] (
        [Id] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [PartNumber] nvarchar(30) NOT NULL,
        [OemNumber] nvarchar(100) NULL,
        [Embedding] vector(1536) NOT NULL,
        [Model] nvarchar(100) NOT NULL,
        [Dimensions] int NOT NULL,
        [SourceText] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [ModifiedBy] nvarchar(max) NOT NULL,
        [Isdeleted] bit NOT NULL,
        CONSTRAINT [PK_ProductEmbeddings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductEmbeddings_Parts_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Parts] ([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [IX_ProductEmbeddings_ProductId] ON [ProductEmbeddings] ([ProductId]);
    ');
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF OBJECT_ID(N'[ProductEmbeddings]', N'U') IS NOT NULL DROP TABLE [ProductEmbeddings];");
        }
    }
}
