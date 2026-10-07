using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class WeeklyFieldBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SeriesId",
                table: "FieldBookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeriesWeeks",
                table: "FieldBookings",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_FieldBookings_SeriesId",
                table: "FieldBookings",
                column: "SeriesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FieldBookings_SeriesId",
                table: "FieldBookings");

            migrationBuilder.DropColumn(
                name: "SeriesId",
                table: "FieldBookings");

            migrationBuilder.DropColumn(
                name: "SeriesWeeks",
                table: "FieldBookings");
        }
    }
}
