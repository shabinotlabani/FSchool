using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class PlayerPerformanceDevelopment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerEvaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    CoachId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Technique = table.Column<int>(type: "integer", nullable: false),
                    Tactics = table.Column<int>(type: "integer", nullable: false),
                    Fitness = table.Column<int>(type: "integer", nullable: false),
                    Discipline = table.Column<int>(type: "integer", nullable: false),
                    Teamwork = table.Column<int>(type: "integer", nullable: false),
                    Strengths = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Improvements = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Goals = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ReviewDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerEvaluations_AspNetUsers_CoachId",
                        column: x => x.CoachId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerEvaluations_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerEvaluations_CoachId",
                table: "PlayerEvaluations",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerEvaluations_StudentId",
                table: "PlayerEvaluations",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerEvaluations");
        }
    }
}
