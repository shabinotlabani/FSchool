using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class AutomaticNutritionDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MenuAttempts",
                table: "CareReferrals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MenuDraft",
                table: "CareReferrals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MenuError",
                table: "CareReferrals",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MenuNextAttempt",
                table: "CareReferrals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MenuStatus",
                table: "CareReferrals",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MenuAttempts",
                table: "CareReferrals");

            migrationBuilder.DropColumn(
                name: "MenuDraft",
                table: "CareReferrals");

            migrationBuilder.DropColumn(
                name: "MenuError",
                table: "CareReferrals");

            migrationBuilder.DropColumn(
                name: "MenuNextAttempt",
                table: "CareReferrals");

            migrationBuilder.DropColumn(
                name: "MenuStatus",
                table: "CareReferrals");
        }
    }
}
