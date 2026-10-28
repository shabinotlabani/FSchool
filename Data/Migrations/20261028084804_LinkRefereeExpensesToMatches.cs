using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkRefereeExpensesToMatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MatchGameId",
                table: "Expenses",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ClubId_MatchGameId",
                table: "Expenses",
                columns: new[] { "ClubId", "MatchGameId" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_MatchGameId",
                table: "Expenses",
                column: "MatchGameId");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_MatchGames_ClubId_MatchGameId",
                table: "Expenses",
                columns: new[] { "ClubId", "MatchGameId" },
                principalTable: "MatchGames",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_MatchGames_MatchGameId",
                table: "Expenses",
                column: "MatchGameId",
                principalTable: "MatchGames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_MatchGames_ClubId_MatchGameId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_MatchGames_MatchGameId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_ClubId_MatchGameId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_MatchGameId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "MatchGameId",
                table: "Expenses");
        }
    }
}
