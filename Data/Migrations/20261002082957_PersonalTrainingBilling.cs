using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class PersonalTrainingBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PersonalChargeId",
                table: "Payments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PersonalTariffs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalTariffs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersonalTrainings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    PersonalTariffId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: true),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    Coach = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalTrainings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonalTrainings_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PersonalTrainings_PersonalTariffs_PersonalTariffId",
                        column: x => x.PersonalTariffId,
                        principalTable: "PersonalTariffs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonalTrainings_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PersonalCharges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonalTrainingId = table.Column<int>(type: "integer", nullable: false),
                    Period = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Paid = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Waived = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Calculation = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    WaiverReason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    WaiverNotes = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalCharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonalCharges_PersonalTrainings_PersonalTrainingId",
                        column: x => x.PersonalTrainingId,
                        principalTable: "PersonalTrainings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PersonalTrainingAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonalTrainingId = table.Column<int>(type: "integer", nullable: true),
                    PersonalTariffId = table.Column<int>(type: "integer", nullable: true),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Snapshot = table.Column<string>(type: "text", nullable: false),
                    ActorId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalTrainingAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonalTrainingAudits_AspNetUsers_ActorId",
                        column: x => x.ActorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PersonalTrainingAudits_PersonalTariffs_PersonalTariffId",
                        column: x => x.PersonalTariffId,
                        principalTable: "PersonalTariffs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PersonalTrainingAudits_PersonalTrainings_PersonalTrainingId",
                        column: x => x.PersonalTrainingId,
                        principalTable: "PersonalTrainings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PersonalTrainingSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonalTrainingId = table.Column<int>(type: "integer", nullable: false),
                    Day = table.Column<int>(type: "integer", nullable: false),
                    StartsAt = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndsAt = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalTrainingSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonalTrainingSlots_PersonalTrainings_PersonalTrainingId",
                        column: x => x.PersonalTrainingId,
                        principalTable: "PersonalTrainings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PersonalChargeId",
                table: "Payments",
                column: "PersonalChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalCharges_PersonalTrainingId_Period",
                table: "PersonalCharges",
                columns: new[] { "PersonalTrainingId", "Period" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTrainingAudits_ActorId",
                table: "PersonalTrainingAudits",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTrainingAudits_PersonalTariffId",
                table: "PersonalTrainingAudits",
                column: "PersonalTariffId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTrainingAudits_PersonalTrainingId",
                table: "PersonalTrainingAudits",
                column: "PersonalTrainingId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTrainings_CreatedByUserId",
                table: "PersonalTrainings",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTrainings_PersonalTariffId",
                table: "PersonalTrainings",
                column: "PersonalTariffId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTrainings_RequestId",
                table: "PersonalTrainings",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTrainings_StudentId",
                table: "PersonalTrainings",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTrainingSlots_PersonalTrainingId",
                table: "PersonalTrainingSlots",
                column: "PersonalTrainingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_PersonalCharges_PersonalChargeId",
                table: "Payments",
                column: "PersonalChargeId",
                principalTable: "PersonalCharges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_PersonalCharges_PersonalChargeId",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "PersonalCharges");

            migrationBuilder.DropTable(
                name: "PersonalTrainingAudits");

            migrationBuilder.DropTable(
                name: "PersonalTrainingSlots");

            migrationBuilder.DropTable(
                name: "PersonalTrainings");

            migrationBuilder.DropTable(
                name: "PersonalTariffs");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PersonalChargeId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PersonalChargeId",
                table: "Payments");
        }
    }
}
