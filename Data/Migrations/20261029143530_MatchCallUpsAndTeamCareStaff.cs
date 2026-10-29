using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class MatchCallUpsAndTeamCareStaff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DoctorId",
                table: "TrainingTeams",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PsychologistId",
                table: "TrainingTeams",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CallUpReason",
                table: "MatchPlayers",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeTeamId",
                table: "MatchPlayers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCallUp",
                table: "MatchPlayers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTeams_DoctorId",
                table: "TrainingTeams",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTeams_PsychologistId",
                table: "TrainingTeams",
                column: "PsychologistId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchPlayers_ClubId_HomeTeamId",
                table: "MatchPlayers",
                columns: new[] { "ClubId", "HomeTeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchPlayers_HomeTeamId",
                table: "MatchPlayers",
                column: "HomeTeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_MatchPlayers_TrainingTeams_ClubId_HomeTeamId",
                table: "MatchPlayers",
                columns: new[] { "ClubId", "HomeTeamId" },
                principalTable: "TrainingTeams",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchPlayers_TrainingTeams_HomeTeamId",
                table: "MatchPlayers",
                column: "HomeTeamId",
                principalTable: "TrainingTeams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingTeams_AspNetUsers_DoctorId",
                table: "TrainingTeams",
                column: "DoctorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingTeams_AspNetUsers_PsychologistId",
                table: "TrainingTeams",
                column: "PsychologistId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchPlayers_TrainingTeams_ClubId_HomeTeamId",
                table: "MatchPlayers");

            migrationBuilder.DropForeignKey(
                name: "FK_MatchPlayers_TrainingTeams_HomeTeamId",
                table: "MatchPlayers");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingTeams_AspNetUsers_DoctorId",
                table: "TrainingTeams");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingTeams_AspNetUsers_PsychologistId",
                table: "TrainingTeams");

            migrationBuilder.DropIndex(
                name: "IX_TrainingTeams_DoctorId",
                table: "TrainingTeams");

            migrationBuilder.DropIndex(
                name: "IX_TrainingTeams_PsychologistId",
                table: "TrainingTeams");

            migrationBuilder.DropIndex(
                name: "IX_MatchPlayers_ClubId_HomeTeamId",
                table: "MatchPlayers");

            migrationBuilder.DropIndex(
                name: "IX_MatchPlayers_HomeTeamId",
                table: "MatchPlayers");

            migrationBuilder.DropColumn(
                name: "DoctorId",
                table: "TrainingTeams");

            migrationBuilder.DropColumn(
                name: "PsychologistId",
                table: "TrainingTeams");

            migrationBuilder.DropColumn(
                name: "CallUpReason",
                table: "MatchPlayers");

            migrationBuilder.DropColumn(
                name: "HomeTeamId",
                table: "MatchPlayers");

            migrationBuilder.DropColumn(
                name: "IsCallUp",
                table: "MatchPlayers");
        }
    }
}
