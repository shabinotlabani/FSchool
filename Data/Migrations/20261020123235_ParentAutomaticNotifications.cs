using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class ParentAutomaticNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentDay",
                table: "ParentPushSettings",
                type: "integer",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<int>(
                name: "PaymentHour",
                table: "ParentPushSettings",
                type: "integer",
                nullable: false,
                defaultValue: 9);

            migrationBuilder.AddColumn<bool>(
                name: "PaymentReminders",
                table: "ParentPushSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ScheduleNotifications",
                table: "ParentPushSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "TrainingReminders",
                table: "ParentPushSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "AutomaticKind",
                table: "ParentPushMessages",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "ParentPushMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScheduleHash",
                table: "ParentPushMessages",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceKey",
                table: "ParentPushMessages",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ParentScheduleStates",
                columns: table => new
                {
                    SourceKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentScheduleStates", x => x.SourceKey);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParentScheduleStates");

            migrationBuilder.DropColumn(
                name: "PaymentDay",
                table: "ParentPushSettings");

            migrationBuilder.DropColumn(
                name: "PaymentHour",
                table: "ParentPushSettings");

            migrationBuilder.DropColumn(
                name: "PaymentReminders",
                table: "ParentPushSettings");

            migrationBuilder.DropColumn(
                name: "ScheduleNotifications",
                table: "ParentPushSettings");

            migrationBuilder.DropColumn(
                name: "TrainingReminders",
                table: "ParentPushSettings");

            migrationBuilder.DropColumn(
                name: "AutomaticKind",
                table: "ParentPushMessages");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "ParentPushMessages");

            migrationBuilder.DropColumn(
                name: "ScheduleHash",
                table: "ParentPushMessages");

            migrationBuilder.DropColumn(
                name: "SourceKey",
                table: "ParentPushMessages");
        }
    }
}
