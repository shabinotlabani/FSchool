using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class StockEntryDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductCode",
                table: "StockEntryDetails",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "StockEntryDetails",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductSize",
                table: "StockEntryDetails",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestId",
                table: "StockEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockEntries_RequestId",
                table: "StockEntries",
                column: "RequestId",
                unique: true);

            migrationBuilder.Sql("""
                UPDATE "StockEntryDetails" AS d
                SET "ProductName" = p."Name", "ProductCode" = p."Code", "ProductSize" = p."Size"
                FROM "Products" AS p WHERE d."ProductId" = p."Id";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockEntries_RequestId",
                table: "StockEntries");

            migrationBuilder.DropColumn(
                name: "ProductCode",
                table: "StockEntryDetails");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "StockEntryDetails");

            migrationBuilder.DropColumn(
                name: "ProductSize",
                table: "StockEntryDetails");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "StockEntries");
        }
    }
}
