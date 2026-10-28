using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerContractAnnexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerContractAnnexes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerContractId = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EffectiveOn = table.Column<DateOnly>(type: "date", nullable: false),
                    PreviousGrossSalary = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NewGrossSalary = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerContractAnnexes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerContractAnnexes_AspNetUsers_ActorId",
                        column: x => x.ActorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerContractAnnexes_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerContractAnnexes_PlayerContracts_ClubId_PlayerContract~",
                        columns: x => new { x.ClubId, x.PlayerContractId },
                        principalTable: "PlayerContracts",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerContractAnnexes_PlayerContracts_PlayerContractId",
                        column: x => x.PlayerContractId,
                        principalTable: "PlayerContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerContractAnnexes_ActorId",
                table: "PlayerContractAnnexes",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerContractAnnexes_ClubId_PlayerContractId_Number",
                table: "PlayerContractAnnexes",
                columns: new[] { "ClubId", "PlayerContractId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerContractAnnexes_PlayerContractId",
                table: "PlayerContractAnnexes",
                column: "PlayerContractId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerContractAnnexes");
        }
    }
}
