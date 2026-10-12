using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class PreferredMonthlyPaymentMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredPaymentMethod",
                table: "Students",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Cash");

            migrationBuilder.AddColumn<string>(
                name: "PreferredPaymentMethod",
                table: "PlayerFamilies",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Cash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredPaymentMethod",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "PreferredPaymentMethod",
                table: "PlayerFamilies");
        }
    }
}
