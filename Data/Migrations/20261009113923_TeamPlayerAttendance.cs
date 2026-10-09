using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class TeamPlayerAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeamAttendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainingTeamId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SavedById = table.Column<string>(type: "text", nullable: false),
                    CoachRole = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamAttendances_AspNetUsers_SavedById",
                        column: x => x.SavedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamAttendances_TrainingTeams_TrainingTeamId",
                        column: x => x.TrainingTeamId,
                        principalTable: "TrainingTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamAttendanceId = table.Column<int>(type: "integer", nullable: false),
                    ActorId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Snapshot = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceChanges_AspNetUsers_ActorId",
                        column: x => x.ActorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceChanges_TeamAttendances_TeamAttendanceId",
                        column: x => x.TeamAttendanceId,
                        principalTable: "TeamAttendances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendancePlayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamAttendanceId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    StudentName = table.Column<string>(type: "character varying(201)", maxLength: 201, nullable: false),
                    Present = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendancePlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendancePlayers_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendancePlayers_TeamAttendances_TeamAttendanceId",
                        column: x => x.TeamAttendanceId,
                        principalTable: "TeamAttendances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceChanges_ActorId",
                table: "AttendanceChanges",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceChanges_TeamAttendanceId",
                table: "AttendanceChanges",
                column: "TeamAttendanceId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendancePlayers_StudentId",
                table: "AttendancePlayers",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendancePlayers_TeamAttendanceId_StudentId",
                table: "AttendancePlayers",
                columns: new[] { "TeamAttendanceId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamAttendances_SavedById",
                table: "TeamAttendances",
                column: "SavedById");

            migrationBuilder.CreateIndex(
                name: "IX_TeamAttendances_TrainingTeamId_Date",
                table: "TeamAttendances",
                columns: new[] { "TrainingTeamId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceChanges");

            migrationBuilder.DropTable(
                name: "AttendancePlayers");

            migrationBuilder.DropTable(
                name: "TeamAttendances");
        }
    }
}
