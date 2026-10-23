using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class CoachMenuApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovedRole",
                table: "NutritionMenus",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MenuForCoach",
                table: "CareReferrals",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE \"NutritionMenus\" SET \"ApprovedRole\" = 'Nutricist' WHERE \"ApprovedRole\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedRole",
                table: "NutritionMenus");

            migrationBuilder.DropColumn(
                name: "MenuForCoach",
                table: "CareReferrals");
        }
    }
}
