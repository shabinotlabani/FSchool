using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class SportDevelopmentSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Sport",
                table: "PlayerEvaluations",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Football");

            migrationBuilder.AddColumn<string>(
                name: "SchemaData",
                table: "DevelopmentRecords",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Sport",
                table: "DevelopmentRecords",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Football");

            migrationBuilder.AddColumn<string>(
                name: "Sport",
                table: "Clubs",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Football");

            migrationBuilder.CreateTable(
                name: "DevelopmentMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Sport = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Section = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Label = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Min = table.Column<decimal>(type: "numeric", nullable: true),
                    Max = table.Column<decimal>(type: "numeric", nullable: true),
                    Step = table.Column<string>(type: "text", nullable: false),
                    Choices = table.Column<string>(type: "text", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevelopmentMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DevelopmentMetrics_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Clubs",
                keyColumn: "Id",
                keyValue: 1,
                column: "Sport",
                value: "Football");

            migrationBuilder.CreateIndex(
                name: "IX_DevelopmentMetrics_ClubId_Sport_Section_Key",
                table: "DevelopmentMetrics",
                columns: new[] { "ClubId", "Sport", "Section", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DevelopmentMetrics");

            migrationBuilder.DropColumn(
                name: "Sport",
                table: "PlayerEvaluations");

            migrationBuilder.DropColumn(
                name: "SchemaData",
                table: "DevelopmentRecords");

            migrationBuilder.DropColumn(
                name: "Sport",
                table: "DevelopmentRecords");

            migrationBuilder.DropColumn(
                name: "Sport",
                table: "Clubs");
        }
    }
}
