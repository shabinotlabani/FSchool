using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class FieldSeriesPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BatchId",
                table: "FieldPayments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BatchState",
                table: "FieldPayments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
            migrationBuilder.CreateIndex(name: "IX_FieldPayments_BatchId", table: "FieldPayments", column: "BatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_FieldPayments_BatchId", table: "FieldPayments");
            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "FieldPayments");

            migrationBuilder.DropColumn(
                name: "BatchState",
                table: "FieldPayments");
        }
    }
}
