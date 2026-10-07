using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class ManagedMonthlyTariffs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FeeNotes",
                table: "MonthlyFeePeriods",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeePlanName",
                table: "MonthlyFeePeriods",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeeReason",
                table: "MonthlyFeePeriods",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeeWaived",
                table: "MonthlyFeePeriods",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "FeePlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsWaiver = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeePlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeePlanPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FeePlanId = table.Column<int>(type: "integer", nullable: false),
                    EffectiveMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeePlanPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeePlanPrices_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FeePlanPrices_FeePlans_FeePlanId",
                        column: x => x.FeePlanId,
                        principalTable: "FeePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentFeeAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    FeePlanId = table.Column<int>(type: "integer", nullable: false),
                    EffectiveMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentFeeAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentFeeAssignments_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentFeeAssignments_FeePlans_FeePlanId",
                        column: x => x.FeePlanId,
                        principalTable: "FeePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentFeeAssignments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "FeePlans",
                columns: new[] { "Id", "IsWaiver", "Name" },
                values: new object[,]
                {
                    { 1, false, "Pagesa Mujore" },
                    { 2, false, "Pagesa Familjare" },
                    { 3, true, "Lirim nga Pagesa" }
                });

            migrationBuilder.InsertData(
                table: "FeePlanPrices",
                columns: new[] { "Id", "Amount", "CreatedAt", "CreatedByUserId", "EffectiveMonth", "FeePlanId", "Reason" },
                values: new object[,]
                {
                    { 1, 50m, new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, new DateOnly(2000, 1, 1), 1, "Tarifa fillestare" },
                    { 2, 35m, new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, new DateOnly(2000, 1, 1), 2, "Tarifa fillestare" },
                    { 3, 0m, new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, new DateOnly(2000, 1, 1), 3, "Lirim nga pagesa" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeePlanPrices_CreatedByUserId",
                table: "FeePlanPrices",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FeePlanPrices_FeePlanId_EffectiveMonth",
                table: "FeePlanPrices",
                columns: new[] { "FeePlanId", "EffectiveMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeeAssignments_CreatedByUserId",
                table: "StudentFeeAssignments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeeAssignments_FeePlanId",
                table: "StudentFeeAssignments",
                column: "FeePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeeAssignments_StudentId_EffectiveMonth",
                table: "StudentFeeAssignments",
                columns: new[] { "StudentId", "EffectiveMonth" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeePlanPrices");

            migrationBuilder.DropTable(
                name: "StudentFeeAssignments");

            migrationBuilder.DropTable(
                name: "FeePlans");

            migrationBuilder.DropColumn(
                name: "FeeNotes",
                table: "MonthlyFeePeriods");

            migrationBuilder.DropColumn(
                name: "FeePlanName",
                table: "MonthlyFeePeriods");

            migrationBuilder.DropColumn(
                name: "FeeReason",
                table: "MonthlyFeePeriods");

            migrationBuilder.DropColumn(
                name: "IsFeeWaived",
                table: "MonthlyFeePeriods");
        }
    }
}
