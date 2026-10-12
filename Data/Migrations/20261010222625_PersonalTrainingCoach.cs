using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class PersonalTrainingCoach : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoachId",
                table: "PersonalTrainings",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTrainings_CoachId",
                table: "PersonalTrainings",
                column: "CoachId");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonalTrainings_AspNetUsers_CoachId",
                table: "PersonalTrainings",
                column: "CoachId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PersonalTrainings_AspNetUsers_CoachId",
                table: "PersonalTrainings");

            migrationBuilder.DropIndex(
                name: "IX_PersonalTrainings_CoachId",
                table: "PersonalTrainings");

            migrationBuilder.DropColumn(
                name: "CoachId",
                table: "PersonalTrainings");
        }
    }
}
