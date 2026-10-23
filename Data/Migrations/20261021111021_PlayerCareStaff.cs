using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class PlayerCareStaff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CareAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StaffId = table.Column<string>(type: "text", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareAssignments_AspNetUsers_StaffId",
                        column: x => x.StaffId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CareAssignments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CareEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssignmentId = table.Column<int>(type: "integer", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Subject = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PrivateNotes = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    TrainingStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CoachInstructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ReviewDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ShareWithParent = table.Column<bool>(type: "boolean", nullable: false),
                    ParentInstructions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareEntries_CareAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "CareAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CareAssignments_StaffId_StudentId_Kind",
                table: "CareAssignments",
                columns: new[] { "StaffId", "StudentId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareAssignments_StudentId",
                table: "CareAssignments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_CareEntries_AssignmentId",
                table: "CareEntries",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CareEntries_RequestId",
                table: "CareEntries",
                column: "RequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CareEntries");

            migrationBuilder.DropTable(
                name: "CareAssignments");
        }
    }
}
