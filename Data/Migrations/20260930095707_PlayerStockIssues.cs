using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class PlayerStockIssues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Sales",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentName",
                table: "Sales",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlayerName",
                table: "Sales",
                type: "character varying(201)",
                maxLength: 201,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestId",
                table: "Sales",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WaiverReason",
                table: "Sales",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "SaleDetails",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductSize",
                table: "SaleDetails",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SaleId",
                table: "Payments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_RequestId",
                table: "Sales",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SaleId",
                table: "Payments",
                column: "SaleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Sales_SaleId",
                table: "Payments",
                column: "SaleId",
                principalTable: "Sales",
                principalColumn: "Id");

            // Freeze names for old documents without changing their financial records.
            migrationBuilder.Sql("""
                UPDATE "SaleDetails" d SET "ProductName" = p."Name", "ProductSize" = p."Size"
                FROM "Products" p WHERE d."ProductId" = p."Id";
                UPDATE "Sales" s SET "PlayerName" = p."FirstName" || ' ' || p."LastName", "ParentName" = p."ParentName"
                FROM "Students" p WHERE s."StudentId" = p."Id";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Sales_SaleId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Sales_RequestId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Payments_SaleId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ParentName",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "PlayerName",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "WaiverReason",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "SaleDetails");

            migrationBuilder.DropColumn(
                name: "ProductSize",
                table: "SaleDetails");

            migrationBuilder.DropColumn(
                name: "SaleId",
                table: "Payments");
        }
    }
}
