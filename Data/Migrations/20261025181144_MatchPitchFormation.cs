using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class MatchPitchFormation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PitchSlot",
                table: "MatchPlayers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Formation",
                table: "MatchGames",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "4-4-2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PitchSlot",
                table: "MatchPlayers");

            migrationBuilder.DropColumn(
                name: "Formation",
                table: "MatchGames");
        }
    }
}
