using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeasonalPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SeasonalMonths",
                table: "Payments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SeasonalRate",
                table: "Payments",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "SeasonalStartMonth",
                table: "Payments",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeasonalMonths",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "SeasonalRate",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "SeasonalStartMonth",
                table: "Payments");
        }
    }
}
