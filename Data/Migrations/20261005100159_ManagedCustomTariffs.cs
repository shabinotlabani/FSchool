using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class ManagedCustomTariffs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "FeePlans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFamily",
                table: "FeePlans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "FamilyTariffAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FamilyId = table.Column<int>(type: "integer", nullable: false),
                    FeePlanId = table.Column<int>(type: "integer", nullable: false),
                    EffectiveMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyTariffAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FamilyTariffAssignments_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FamilyTariffAssignments_FeePlans_FeePlanId",
                        column: x => x.FeePlanId,
                        principalTable: "FeePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FamilyTariffAssignments_PlayerFamilies_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "PlayerFamilies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "FeePlans",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "IsActive", "IsFamily" },
                values: new object[] { true, false });

            migrationBuilder.UpdateData(
                table: "FeePlans",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "IsActive", "IsFamily" },
                values: new object[] { true, true });

            migrationBuilder.UpdateData(
                table: "FeePlans",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "IsActive", "IsFamily" },
                values: new object[] { true, false });

            migrationBuilder.CreateIndex(
                name: "IX_FamilyTariffAssignments_CreatedByUserId",
                table: "FamilyTariffAssignments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyTariffAssignments_FamilyId_EffectiveMonth",
                table: "FamilyTariffAssignments",
                columns: new[] { "FamilyId", "EffectiveMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_FamilyTariffAssignments_FeePlanId",
                table: "FamilyTariffAssignments",
                column: "FeePlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FamilyTariffAssignments");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "FeePlans");

            migrationBuilder.DropColumn(
                name: "IsFamily",
                table: "FeePlans");
        }
    }
}
