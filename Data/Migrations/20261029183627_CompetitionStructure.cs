using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompetitionStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "TrainingTeams",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Students",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompetitionStage",
                table: "MatchGames",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Group",
                table: "LeagueTeams",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RegisteredOn",
                table: "LeagueTeams",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "SquadDeadline",
                table: "LeagueTeams",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Stage",
                table: "LeagueTeams",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "LeagueTeams",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AgeCategory",
                table: "Leagues",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompetitionScope",
                table: "Leagues",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompetitionType",
                table: "Leagues",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Leagues",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Division",
                table: "Leagues",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Leagues",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Group",
                table: "Leagues",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOfficial",
                table: "Leagues",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "Leagues",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Organizer",
                table: "Leagues",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Season",
                table: "Leagues",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Sport",
                table: "Leagues",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Stage",
                table: "Leagues",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE "TrainingTeams" SET "Gender" = 'Male' WHERE "Gender" = '';
                UPDATE "Students" SET "Gender" = 'Male' WHERE "Gender" = '';
                UPDATE "Leagues" SET
                    "Country" = 'Kosovë', "Organizer" = 'FFK', "Sport" = 'Football',
                    "Gender" = 'Male', "AgeCategory" = 'Senior', "CompetitionScope" = 'Domestic',
                    "CompetitionType" = CASE WHEN "Kind" = 'Cup' THEN 'Cup' WHEN "Kind" = 'Friendly' THEN 'Friendly' ELSE 'League' END,
                    "Stage" = CASE WHEN "Kind" = 'Cup' THEN 'Knockout' ELSE 'LeaguePhase' END,
                    "Season" = EXTRACT(YEAR FROM "StartsOn")::text || '/' || RIGHT(EXTRACT(YEAR FROM "EndsOn")::text, 2),
                    "IsOfficial" = CASE WHEN "Kind" <> 'Friendly' THEN TRUE ELSE FALSE END,
                    "IsVerified" = CASE WHEN "Kind" <> 'Friendly' THEN TRUE ELSE FALSE END
                WHERE "Country" = '';
                UPDATE "LeagueTeams" lt SET
                    "Status" = 'Registered', "RegisteredOn" = l."StartsOn", "Stage" = l."Stage", "Group" = l."Group"
                FROM "Leagues" l WHERE lt."LeagueId" = l."Id" AND lt."Status" = '';
                UPDATE "MatchGames" m SET "CompetitionStage" = l."Stage"
                FROM "Leagues" l WHERE m."LeagueId" = l."Id" AND m."CompetitionStage" IS NULL;
                """);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_LeagueTeams_ClubId_Id",
                table: "LeagueTeams",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.CreateTable(
                name: "CompetitionPlayerRegistrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeagueTeamId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    ListType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RegisteredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EligibleFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    EligibleUntil = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ClubId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitionPlayerRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetitionPlayerRegistrations_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompetitionPlayerRegistrations_LeagueTeams_ClubId_LeagueTea~",
                        columns: x => new { x.ClubId, x.LeagueTeamId },
                        principalTable: "LeagueTeams",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompetitionPlayerRegistrations_LeagueTeams_LeagueTeamId",
                        column: x => x.LeagueTeamId,
                        principalTable: "LeagueTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompetitionPlayerRegistrations_Students_ClubId_StudentId",
                        columns: x => new { x.ClubId, x.StudentId },
                        principalTable: "Students",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompetitionPlayerRegistrations_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionPlayerRegistrations_ClubId_LeagueTeamId_StudentId",
                table: "CompetitionPlayerRegistrations",
                columns: new[] { "ClubId", "LeagueTeamId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionPlayerRegistrations_ClubId_StudentId",
                table: "CompetitionPlayerRegistrations",
                columns: new[] { "ClubId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionPlayerRegistrations_LeagueTeamId",
                table: "CompetitionPlayerRegistrations",
                column: "LeagueTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionPlayerRegistrations_StudentId",
                table: "CompetitionPlayerRegistrations",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompetitionPlayerRegistrations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_LeagueTeams_ClubId_Id",
                table: "LeagueTeams");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "TrainingTeams");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "CompetitionStage",
                table: "MatchGames");

            migrationBuilder.DropColumn(
                name: "Group",
                table: "LeagueTeams");

            migrationBuilder.DropColumn(
                name: "RegisteredOn",
                table: "LeagueTeams");

            migrationBuilder.DropColumn(
                name: "SquadDeadline",
                table: "LeagueTeams");

            migrationBuilder.DropColumn(
                name: "Stage",
                table: "LeagueTeams");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "LeagueTeams");

            migrationBuilder.DropColumn(
                name: "AgeCategory",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "CompetitionScope",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "CompetitionType",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "Division",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "Group",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "IsOfficial",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "Organizer",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "Season",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "Sport",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "Stage",
                table: "Leagues");
        }
    }
}
