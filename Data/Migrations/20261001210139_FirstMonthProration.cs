using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class FirstMonthProration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FullMonthlyAmount",
                table: "MonthlyFeePeriods",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ProratedFrom",
                table: "MonthlyFeePeriods",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FullMonthlyAmount",
                table: "MonthlyFeePeriods");

            migrationBuilder.DropColumn(
                name: "ProratedFrom",
                table: "MonthlyFeePeriods");
        }
    }
}
