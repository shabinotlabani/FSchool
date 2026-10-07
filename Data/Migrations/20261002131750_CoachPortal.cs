using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class CoachPortal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Specialization",
                table: "AspNetUsers",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CoachTeams",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    TrainingTeamId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachTeams", x => new { x.UserId, x.TrainingTeamId });
                    table.ForeignKey(
                        name: "FK_CoachTeams_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoachTeams_TrainingTeams_TrainingTeamId",
                        column: x => x.TrainingTeamId,
                        principalTable: "TrainingTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoachTeams_TrainingTeamId",
                table: "CoachTeams",
                column: "TrainingTeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoachTeams");

            migrationBuilder.DropColumn(
                name: "Specialization",
                table: "AspNetUsers");
        }
    }
}
