using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class TeamHeadAndAssistantCoaches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssistantCoachId",
                table: "TrainingTeams",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoachId",
                table: "TrainingTeams",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTeams_AssistantCoachId",
                table: "TrainingTeams",
                column: "AssistantCoachId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTeams_CoachId",
                table: "TrainingTeams",
                column: "CoachId");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingTeams_AspNetUsers_AssistantCoachId",
                table: "TrainingTeams",
                column: "AssistantCoachId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingTeams_AspNetUsers_CoachId",
                table: "TrainingTeams",
                column: "CoachId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainingTeams_AspNetUsers_AssistantCoachId",
                table: "TrainingTeams");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingTeams_AspNetUsers_CoachId",
                table: "TrainingTeams");

            migrationBuilder.DropIndex(
                name: "IX_TrainingTeams_AssistantCoachId",
                table: "TrainingTeams");

            migrationBuilder.DropIndex(
                name: "IX_TrainingTeams_CoachId",
                table: "TrainingTeams");

            migrationBuilder.DropColumn(
                name: "AssistantCoachId",
                table: "TrainingTeams");

            migrationBuilder.DropColumn(
                name: "CoachId",
                table: "TrainingTeams");
        }
    }
}
