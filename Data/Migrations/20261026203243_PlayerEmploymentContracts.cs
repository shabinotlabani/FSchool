using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class PlayerEmploymentContracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StudentId",
                table: "SponsorshipBeneficiaries",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlayerContracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    BeneficiaryId = table.Column<int>(type: "integer", nullable: true),
                    Number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GrossSalary = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TermsData = table.Column<string>(type: "text", nullable: false),
                    SignedPdf = table.Column<byte[]>(type: "bytea", nullable: true),
                    FileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActorId = table.Column<string>(type: "text", nullable: false),
                    EndReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ClubId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerContracts", x => x.Id);
                    table.UniqueConstraint("AK_PlayerContracts_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_PlayerContracts_AspNetUsers_ActorId",
                        column: x => x.ActorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerContracts_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerContracts_SponsorshipBeneficiaries_BeneficiaryId",
                        column: x => x.BeneficiaryId,
                        principalTable: "SponsorshipBeneficiaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerContracts_SponsorshipBeneficiaries_ClubId_Beneficiary~",
                        columns: x => new { x.ClubId, x.BeneficiaryId },
                        principalTable: "SponsorshipBeneficiaries",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerContracts_Students_ClubId_StudentId",
                        columns: x => new { x.ClubId, x.StudentId },
                        principalTable: "Students",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerContracts_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerContractAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerContractId = table.Column<int>(type: "integer", nullable: false),
                    At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Action = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Snapshot = table.Column<string>(type: "text", nullable: false),
                    ClubId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerContractAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerContractAudits_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerContractAudits_PlayerContracts_ClubId_PlayerContractId",
                        columns: x => new { x.ClubId, x.PlayerContractId },
                        principalTable: "PlayerContracts",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerContractAudits_PlayerContracts_PlayerContractId",
                        column: x => x.PlayerContractId,
                        principalTable: "PlayerContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipBeneficiaries_ClubId_StudentId",
                table: "SponsorshipBeneficiaries",
                columns: new[] { "ClubId", "StudentId" },
                unique: true,
                filter: "\"StudentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipBeneficiaries_StudentId",
                table: "SponsorshipBeneficiaries",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerContractAudits_ClubId_PlayerContractId",
                table: "PlayerContractAudits",
                columns: new[] { "ClubId", "PlayerContractId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerContractAudits_PlayerContractId",
                table: "PlayerContractAudits",
                column: "PlayerContractId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerContracts_ActorId",
                table: "PlayerContracts",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerContracts_BeneficiaryId",
                table: "PlayerContracts",
                column: "BeneficiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerContracts_ClubId_BeneficiaryId",
                table: "PlayerContracts",
                columns: new[] { "ClubId", "BeneficiaryId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerContracts_ClubId_Number",
                table: "PlayerContracts",
                columns: new[] { "ClubId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerContracts_ClubId_StudentId",
                table: "PlayerContracts",
                columns: new[] { "ClubId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerContracts_RequestId",
                table: "PlayerContracts",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerContracts_StudentId",
                table: "PlayerContracts",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_SponsorshipBeneficiaries_Students_ClubId_StudentId",
                table: "SponsorshipBeneficiaries",
                columns: new[] { "ClubId", "StudentId" },
                principalTable: "Students",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SponsorshipBeneficiaries_Students_StudentId",
                table: "SponsorshipBeneficiaries",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SponsorshipBeneficiaries_Students_ClubId_StudentId",
                table: "SponsorshipBeneficiaries");

            migrationBuilder.DropForeignKey(
                name: "FK_SponsorshipBeneficiaries_Students_StudentId",
                table: "SponsorshipBeneficiaries");

            migrationBuilder.DropTable(
                name: "PlayerContractAudits");

            migrationBuilder.DropTable(
                name: "PlayerContracts");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipBeneficiaries_ClubId_StudentId",
                table: "SponsorshipBeneficiaries");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipBeneficiaries_StudentId",
                table: "SponsorshipBeneficiaries");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "SponsorshipBeneficiaries");
        }
    }
}
