using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class ManagedLeagues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LeagueId",
                table: "MatchGames",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Leagues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RulesConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    YellowThreshold = table.Column<int>(type: "integer", nullable: false),
                    YellowBan = table.Column<int>(type: "integer", nullable: false),
                    RedBan = table.Column<int>(type: "integer", nullable: false),
                    SecondYellowBan = table.Column<int>(type: "integer", nullable: false),
                    CountDismissalYellows = table.Column<bool>(type: "boolean", nullable: false),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedById = table.Column<string>(type: "text", nullable: false),
                    ClubId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leagues", x => x.Id);
                    table.UniqueConstraint("AK_Leagues_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_Leagues_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leagues_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeagueTeams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeagueId = table.Column<int>(type: "integer", nullable: false),
                    TrainingTeamId = table.Column<int>(type: "integer", nullable: false),
                    ClubId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeagueTeams_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeagueTeams_Leagues_ClubId_LeagueId",
                        columns: x => new { x.ClubId, x.LeagueId },
                        principalTable: "Leagues",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeagueTeams_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeagueTeams_TrainingTeams_ClubId_TrainingTeamId",
                        columns: x => new { x.ClubId, x.TrainingTeamId },
                        principalTable: "TrainingTeams",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeagueTeams_TrainingTeams_TrainingTeamId",
                        column: x => x.TrainingTeamId,
                        principalTable: "TrainingTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchGames_ClubId_LeagueId",
                table: "MatchGames",
                columns: new[] { "ClubId", "LeagueId" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchGames_LeagueId",
                table: "MatchGames",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_ClubId_Name_StartsOn",
                table: "Leagues",
                columns: new[] { "ClubId", "Name", "StartsOn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_UpdatedById",
                table: "Leagues",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_LeagueTeams_ClubId_LeagueId_TrainingTeamId",
                table: "LeagueTeams",
                columns: new[] { "ClubId", "LeagueId", "TrainingTeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeagueTeams_ClubId_TrainingTeamId",
                table: "LeagueTeams",
                columns: new[] { "ClubId", "TrainingTeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_LeagueTeams_LeagueId",
                table: "LeagueTeams",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_LeagueTeams_TrainingTeamId",
                table: "LeagueTeams",
                column: "TrainingTeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_MatchGames_Leagues_ClubId_LeagueId",
                table: "MatchGames",
                columns: new[] { "ClubId", "LeagueId" },
                principalTable: "Leagues",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchGames_Leagues_LeagueId",
                table: "MatchGames",
                column: "LeagueId",
                principalTable: "Leagues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchGames_Leagues_ClubId_LeagueId",
                table: "MatchGames");

            migrationBuilder.DropForeignKey(
                name: "FK_MatchGames_Leagues_LeagueId",
                table: "MatchGames");

            migrationBuilder.DropTable(
                name: "LeagueTeams");

            migrationBuilder.DropTable(
                name: "Leagues");

            migrationBuilder.DropIndex(
                name: "IX_MatchGames_ClubId_LeagueId",
                table: "MatchGames");

            migrationBuilder.DropIndex(
                name: "IX_MatchGames_LeagueId",
                table: "MatchGames");

            migrationBuilder.DropColumn(
                name: "LeagueId",
                table: "MatchGames");
        }
    }
}
