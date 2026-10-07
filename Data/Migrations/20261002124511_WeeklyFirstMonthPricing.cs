using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class WeeklyFirstMonthPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FirstMonthFinal",
                table: "MonthlyFeePeriods",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstMonthReason",
                table: "MonthlyFeePeriods",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FirstMonthSuggested",
                table: "MonthlyFeePeriods",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FirstMonthWeeks",
                table: "MonthlyFeePeriods",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FirstMonthFeeChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodId = table.Column<int>(type: "integer", nullable: false),
                    PreviousAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ActorId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FirstMonthFeeChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FirstMonthFeeChanges_AspNetUsers_ActorId",
                        column: x => x.ActorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FirstMonthFeeChanges_MonthlyFeePeriods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "MonthlyFeePeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FirstMonthFeeChanges_ActorId",
                table: "FirstMonthFeeChanges",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_FirstMonthFeeChanges_PeriodId",
                table: "FirstMonthFeeChanges",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_FirstMonthFeeChanges_RequestId",
                table: "FirstMonthFeeChanges",
                column: "RequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FirstMonthFeeChanges");

            migrationBuilder.DropColumn(
                name: "FirstMonthFinal",
                table: "MonthlyFeePeriods");

            migrationBuilder.DropColumn(
                name: "FirstMonthReason",
                table: "MonthlyFeePeriods");

            migrationBuilder.DropColumn(
                name: "FirstMonthSuggested",
                table: "MonthlyFeePeriods");

            migrationBuilder.DropColumn(
                name: "FirstMonthWeeks",
                table: "MonthlyFeePeriods");
        }
    }
}
