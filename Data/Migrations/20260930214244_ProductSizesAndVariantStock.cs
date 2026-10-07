using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProductSizesAndVariantStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockEntryDetails_ProductId",
                table: "StockEntryDetails");

            migrationBuilder.DropIndex(
                name: "IX_SaleDetails_ProductId",
                table: "SaleDetails");

            migrationBuilder.AddColumn<string>(
                name: "ProductSize",
                table: "StockMovements",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductVariantId",
                table: "StockMovements",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitName",
                table: "StockMovements",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductVariantId",
                table: "StockEntryDetails",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitName",
                table: "StockEntryDetails",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductVariantId",
                table: "SaleDetails",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitName",
                table: "SaleDetails",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitName",
                table: "Products",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Size = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentStock = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.Id);
                    table.UniqueConstraint("AK_ProductVariants_ProductId_Id", x => new { x.ProductId, x.Id });
                    table.ForeignKey(
                        name: "FK_ProductVariants_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SizeGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Sizes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SizeGroups", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SizeGroups",
                columns: new[] { "Id", "IsActive", "Name", "Sizes" },
                values: new object[,]
                {
                    { 1, true, "Veshje sportive", "6, 8, 10, 12, 14, S, M, L, XL, XXL" },
                    { 2, true, "Qorape", "27–30, 31–34, 35–38, 39–42, 43–46" },
                    { 3, true, "Pa madhësi", "Standard" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductId_ProductVariantId",
                table: "StockMovements",
                columns: new[] { "ProductId", "ProductVariantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockEntryDetails_ProductId_ProductVariantId",
                table: "StockEntryDetails",
                columns: new[] { "ProductId", "ProductVariantId" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleDetails_ProductId_ProductVariantId",
                table: "SaleDetails",
                columns: new[] { "ProductId", "ProductVariantId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_Size",
                table: "ProductVariants",
                columns: new[] { "ProductId", "Size" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleDetails_ProductVariants_ProductId_ProductVariantId",
                table: "SaleDetails",
                columns: new[] { "ProductId", "ProductVariantId" },
                principalTable: "ProductVariants",
                principalColumns: new[] { "ProductId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockEntryDetails_ProductVariants_ProductId_ProductVariantId",
                table: "StockEntryDetails",
                columns: new[] { "ProductId", "ProductVariantId" },
                principalTable: "ProductVariants",
                principalColumns: new[] { "ProductId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_ProductVariants_ProductId_ProductVariantId",
                table: "StockMovements",
                columns: new[] { "ProductId", "ProductVariantId" },
                principalTable: "ProductVariants",
                principalColumns: new[] { "ProductId", "Id" },
                onDelete: ReferentialAction.Restrict);
            // Preserve each existing product's stock and historical document snapshots.
            migrationBuilder.Sql("""
                UPDATE "Products" SET "UnitName" = 'Copë' WHERE "UnitName" = '';
                INSERT INTO "ProductVariants" ("ProductId", "Size", "IsActive", "CurrentStock", "PurchasePrice")
                SELECT "Id", COALESCE(NULLIF(BTRIM("Size"), ''), 'Pa madhësi (vjetër)'), "IsActive", "CurrentStock", "PurchasePrice" FROM "Products";
                UPDATE "StockEntryDetails" d SET "ProductVariantId" = v."Id", "UnitName" = 'Copë' FROM "ProductVariants" v WHERE d."ProductId" = v."ProductId";
                UPDATE "SaleDetails" d SET "ProductVariantId" = v."Id", "UnitName" = 'Copë' FROM "ProductVariants" v WHERE d."ProductId" = v."ProductId";
                UPDATE "StockMovements" m SET "ProductVariantId" = v."Id", "ProductSize" = v."Size", "UnitName" = 'Copë' FROM "ProductVariants" v WHERE m."ProductId" = v."ProductId";
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SaleDetails_ProductVariants_ProductId_ProductVariantId",
                table: "SaleDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_StockEntryDetails_ProductVariants_ProductId_ProductVariantId",
                table: "StockEntryDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_ProductVariants_ProductId_ProductVariantId",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropTable(
                name: "SizeGroups");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ProductId_ProductVariantId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockEntryDetails_ProductId_ProductVariantId",
                table: "StockEntryDetails");

            migrationBuilder.DropIndex(
                name: "IX_SaleDetails_ProductId_ProductVariantId",
                table: "SaleDetails");

            migrationBuilder.DropColumn(
                name: "ProductSize",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "ProductVariantId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "UnitName",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "ProductVariantId",
                table: "StockEntryDetails");

            migrationBuilder.DropColumn(
                name: "UnitName",
                table: "StockEntryDetails");

            migrationBuilder.DropColumn(
                name: "ProductVariantId",
                table: "SaleDetails");

            migrationBuilder.DropColumn(
                name: "UnitName",
                table: "SaleDetails");

            migrationBuilder.DropColumn(
                name: "UnitName",
                table: "Products");

            migrationBuilder.CreateIndex(
                name: "IX_StockEntryDetails_ProductId",
                table: "StockEntryDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleDetails_ProductId",
                table: "SaleDetails",
                column: "ProductId");
        }
    }
}
