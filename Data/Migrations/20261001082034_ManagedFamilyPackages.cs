using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class ManagedFamilyPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FamilyId",
                table: "StudentFeeAssignments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FamilyOrder",
                table: "StudentFeeAssignments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FamilyAdditionalAmount",
                table: "FeePlanPrices",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FamilyFirstAmount",
                table: "FeePlanPrices",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FamilyFirstCount",
                table: "FeePlanPrices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "SingleUsesStandard",
                table: "FeePlanPrices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PlayerFamilies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerFamilies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FamilyChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FamilyId = table.Column<int>(type: "integer", nullable: false),
                    EffectiveMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    Snapshot = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ActorId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FamilyChanges_AspNetUsers_ActorId",
                        column: x => x.ActorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FamilyChanges_PlayerFamilies_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "PlayerFamilies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "FeePlanPrices",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FamilyAdditionalAmount", "FamilyFirstAmount", "FamilyFirstCount", "SingleUsesStandard" },
                values: new object[] { null, null, 2, true });

            migrationBuilder.UpdateData(
                table: "FeePlanPrices",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "FamilyAdditionalAmount", "FamilyFirstAmount", "FamilyFirstCount", "SingleUsesStandard" },
                values: new object[] { 35m, 40m, 2, true });

            migrationBuilder.UpdateData(
                table: "FeePlanPrices",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "FamilyAdditionalAmount", "FamilyFirstAmount", "FamilyFirstCount", "SingleUsesStandard" },
                values: new object[] { null, null, 2, true });

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeeAssignments_FamilyId_EffectiveMonth",
                table: "StudentFeeAssignments",
                columns: new[] { "FamilyId", "EffectiveMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_FamilyChanges_ActorId",
                table: "FamilyChanges",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyChanges_FamilyId",
                table: "FamilyChanges",
                column: "FamilyId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentFeeAssignments_PlayerFamilies_FamilyId",
                table: "StudentFeeAssignments",
                column: "FamilyId",
                principalTable: "PlayerFamilies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("""
                UPDATE "FeePlanPrices" SET "FamilyFirstCount"=2, "SingleUsesStandard"=TRUE;
                UPDATE "FeePlanPrices" SET "FamilyFirstAmount"=40, "FamilyAdditionalAmount"=35 WHERE "FeePlanId"=2;
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentFeeAssignments_PlayerFamilies_FamilyId",
                table: "StudentFeeAssignments");

            migrationBuilder.DropTable(
                name: "FamilyChanges");

            migrationBuilder.DropTable(
                name: "PlayerFamilies");

            migrationBuilder.DropIndex(
                name: "IX_StudentFeeAssignments_FamilyId_EffectiveMonth",
                table: "StudentFeeAssignments");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                table: "StudentFeeAssignments");

            migrationBuilder.DropColumn(
                name: "FamilyOrder",
                table: "StudentFeeAssignments");

            migrationBuilder.DropColumn(
                name: "FamilyAdditionalAmount",
                table: "FeePlanPrices");

            migrationBuilder.DropColumn(
                name: "FamilyFirstAmount",
                table: "FeePlanPrices");

            migrationBuilder.DropColumn(
                name: "FamilyFirstCount",
                table: "FeePlanPrices");

            migrationBuilder.DropColumn(
                name: "SingleUsesStandard",
                table: "FeePlanPrices");
        }
    }
}
