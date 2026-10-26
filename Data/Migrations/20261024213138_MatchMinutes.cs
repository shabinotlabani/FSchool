using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class MatchMinutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchGames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainingTeamId = table.Column<int>(type: "integer", nullable: false),
                    Sport = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Opponent = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Competition = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Venue = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Home = table.Column<bool>(type: "boolean", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Kickoff = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    Duration = table.Column<int>(type: "integer", nullable: false),
                    PlayersOnCourt = table.Column<int>(type: "integer", nullable: false),
                    ScoreFor = table.Column<int>(type: "integer", nullable: true),
                    ScoreAgainst = table.Column<int>(type: "integer", nullable: true),
                    UpdatedById = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClubId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchGames", x => x.Id);
                    table.UniqueConstraint("AK_MatchGames_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_MatchGames_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchGames_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchGames_TrainingTeams_ClubId_TrainingTeamId",
                        columns: x => new { x.ClubId, x.TrainingTeamId },
                        principalTable: "TrainingTeams",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchGames_TrainingTeams_TrainingTeamId",
                        column: x => x.TrainingTeamId,
                        principalTable: "TrainingTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchGameId = table.Column<int>(type: "integer", nullable: false),
                    ActorId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Snapshot = table.Column<string>(type: "text", nullable: false),
                    ClubId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchChanges_AspNetUsers_ActorId",
                        column: x => x.ActorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchChanges_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchChanges_MatchGames_ClubId_MatchGameId",
                        columns: x => new { x.ClubId, x.MatchGameId },
                        principalTable: "MatchGames",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchChanges_MatchGames_MatchGameId",
                        column: x => x.MatchGameId,
                        principalTable: "MatchGames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchPlayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchGameId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(201)", maxLength: 201, nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Minutes = table.Column<int>(type: "integer", nullable: false),
                    Intervals = table.Column<string>(type: "text", nullable: false),
                    ClubId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchPlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchPlayers_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchPlayers_MatchGames_ClubId_MatchGameId",
                        columns: x => new { x.ClubId, x.MatchGameId },
                        principalTable: "MatchGames",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchPlayers_MatchGames_MatchGameId",
                        column: x => x.MatchGameId,
                        principalTable: "MatchGames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchPlayers_Students_ClubId_StudentId",
                        columns: x => new { x.ClubId, x.StudentId },
                        principalTable: "Students",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchPlayers_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchChanges_ActorId",
                table: "MatchChanges",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchChanges_ClubId_MatchGameId",
                table: "MatchChanges",
                columns: new[] { "ClubId", "MatchGameId" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchChanges_MatchGameId",
                table: "MatchChanges",
                column: "MatchGameId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchGames_ClubId_TrainingTeamId",
                table: "MatchGames",
                columns: new[] { "ClubId", "TrainingTeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchGames_RequestId",
                table: "MatchGames",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchGames_TrainingTeamId_Date",
                table: "MatchGames",
                columns: new[] { "TrainingTeamId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchGames_UpdatedById",
                table: "MatchGames",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MatchPlayers_ClubId_MatchGameId_StudentId",
                table: "MatchPlayers",
                columns: new[] { "ClubId", "MatchGameId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchPlayers_ClubId_StudentId",
                table: "MatchPlayers",
                columns: new[] { "ClubId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchPlayers_MatchGameId",
                table: "MatchPlayers",
                column: "MatchGameId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchPlayers_StudentId",
                table: "MatchPlayers",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchChanges");

            migrationBuilder.DropTable(
                name: "MatchPlayers");

            migrationBuilder.DropTable(
                name: "MatchGames");
        }
    }
}
