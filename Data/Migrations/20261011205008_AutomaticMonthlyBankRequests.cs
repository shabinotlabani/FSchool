using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class AutomaticMonthlyBankRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "AutomaticMonth",
                table: "PendingBankPayments",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAutomatic",
                table: "PendingBankPayments",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutomaticMonth",
                table: "PendingBankPayments");

            migrationBuilder.DropColumn(
                name: "IsAutomatic",
                table: "PendingBankPayments");
        }
    }
}
