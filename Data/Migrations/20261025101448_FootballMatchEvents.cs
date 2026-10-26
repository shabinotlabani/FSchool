using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class FootballMatchEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EventData",
                table: "MatchGames",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateTable(
                name: "DisciplineRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainingTeamId = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    YellowThreshold = table.Column<int>(type: "integer", nullable: false),
                    YellowBan = table.Column<int>(type: "integer", nullable: false),
                    RedBan = table.Column<int>(type: "integer", nullable: false),
                    SecondYellowBan = table.Column<int>(type: "integer", nullable: false),
                    CountDismissalYellows = table.Column<bool>(type: "boolean", nullable: false),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedById = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClubId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisciplineRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisciplineRules_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DisciplineRules_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DisciplineRules_TrainingTeams_ClubId_TrainingTeamId",
                        columns: x => new { x.ClubId, x.TrainingTeamId },
                        principalTable: "TrainingTeams",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DisciplineRules_TrainingTeams_TrainingTeamId",
                        column: x => x.TrainingTeamId,
                        principalTable: "TrainingTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DisciplineRules_ClubId_TrainingTeamId_Kind_StartsOn",
                table: "DisciplineRules",
                columns: new[] { "ClubId", "TrainingTeamId", "Kind", "StartsOn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DisciplineRules_TrainingTeamId",
                table: "DisciplineRules",
                column: "TrainingTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplineRules_UpdatedById",
                table: "DisciplineRules",
                column: "UpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DisciplineRules");

            migrationBuilder.DropColumn(
                name: "EventData",
                table: "MatchGames");
        }
    }
}
