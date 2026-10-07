using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class TrainingTeamsAndSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TrainingTeamId",
                table: "Students",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StudentTeamChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    FromTeamId = table.Column<int>(type: "integer", nullable: true),
                    ToTeamId = table.Column<int>(type: "integer", nullable: true),
                    FromTeamName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ToTeamName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ActorId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentTeamChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentTeamChanges_AspNetUsers_ActorId",
                        column: x => x.ActorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentTeamChanges_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingTeams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MinAge = table.Column<int>(type: "integer", nullable: false),
                    MaxAge = table.Column<int>(type: "integer", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingTeams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeamChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainingTeamId = table.Column<int>(type: "integer", nullable: false),
                    Snapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ActorId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamChanges_AspNetUsers_ActorId",
                        column: x => x.ActorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeamChanges_TrainingTeams_TrainingTeamId",
                        column: x => x.TrainingTeamId,
                        principalTable: "TrainingTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainingTeamId = table.Column<int>(type: "integer", nullable: false),
                    Day = table.Column<int>(type: "integer", nullable: false),
                    StartsAt = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndsAt = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingSessions_TrainingTeams_TrainingTeamId",
                        column: x => x.TrainingTeamId,
                        principalTable: "TrainingTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Students_TrainingTeamId",
                table: "Students",
                column: "TrainingTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentTeamChanges_ActorId",
                table: "StudentTeamChanges",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentTeamChanges_StudentId",
                table: "StudentTeamChanges",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamChanges_ActorId",
                table: "TeamChanges",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamChanges_TrainingTeamId",
                table: "TeamChanges",
                column: "TrainingTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessions_TrainingTeamId_Day_StartsAt",
                table: "TrainingSessions",
                columns: new[] { "TrainingTeamId", "Day", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTeams_IsActive",
                table: "TrainingTeams",
                column: "IsActive");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_TrainingTeams_TrainingTeamId",
                table: "Students",
                column: "TrainingTeamId",
                principalTable: "TrainingTeams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_TrainingTeams_TrainingTeamId",
                table: "Students");

            migrationBuilder.DropTable(
                name: "StudentTeamChanges");

            migrationBuilder.DropTable(
                name: "TeamChanges");

            migrationBuilder.DropTable(
                name: "TrainingSessions");

            migrationBuilder.DropTable(
                name: "TrainingTeams");

            migrationBuilder.DropIndex(
                name: "IX_Students_TrainingTeamId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "TrainingTeamId",
                table: "Students");
        }
    }
}
