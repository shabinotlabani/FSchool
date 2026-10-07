using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class FootballFieldsAndPrivateBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FootballFieldId",
                table: "TrainingTeams",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FootballFields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FootballFields", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FieldBookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    FootballFieldId = table.Column<int>(type: "integer", nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    StartsAt = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndsAt = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    CancellationReason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldBookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldBookings_FootballFields_FootballFieldId",
                        column: x => x.FootballFieldId,
                        principalTable: "FootballFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FieldBookingChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FieldBookingId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
                    ActorId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldBookingChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldBookingChanges_AspNetUsers_ActorId",
                        column: x => x.ActorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldBookingChanges_FieldBookings_FieldBookingId",
                        column: x => x.FieldBookingId,
                        principalTable: "FieldBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FieldPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldBookingId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ActorId = table.Column<string>(type: "text", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldPayments_AspNetUsers_ActorId",
                        column: x => x.ActorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldPayments_FieldBookings_FieldBookingId",
                        column: x => x.FieldBookingId,
                        principalTable: "FieldBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "FootballFields",
                columns: new[] { "Id", "IsActive", "Location", "Name", "Notes", "Revision" },
                values: new object[,]
                {
                    { 1, true, null, "Fusha 1", null, new Guid("a6111111-1111-4111-8111-111111111111") },
                    { 2, true, null, "Fusha 2", null, new Guid("a6222222-2222-4222-8222-222222222222") },
                    { 3, true, null, "Fusha 3", null, new Guid("a6333333-3333-4333-8333-333333333333") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTeams_FootballFieldId",
                table: "TrainingTeams",
                column: "FootballFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldBookingChanges_ActorId",
                table: "FieldBookingChanges",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldBookingChanges_FieldBookingId",
                table: "FieldBookingChanges",
                column: "FieldBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldBookings_FootballFieldId_Date_StartsAt",
                table: "FieldBookings",
                columns: new[] { "FootballFieldId", "Date", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldBookings_RequestId",
                table: "FieldBookings",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FieldPayments_ActorId",
                table: "FieldPayments",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldPayments_FieldBookingId",
                table: "FieldPayments",
                column: "FieldBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldPayments_RequestId",
                table: "FieldPayments",
                column: "RequestId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingTeams_FootballFields_FootballFieldId",
                table: "TrainingTeams",
                column: "FootballFieldId",
                principalTable: "FootballFields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainingTeams_FootballFields_FootballFieldId",
                table: "TrainingTeams");

            migrationBuilder.DropTable(
                name: "FieldBookingChanges");

            migrationBuilder.DropTable(
                name: "FieldPayments");

            migrationBuilder.DropTable(
                name: "FieldBookings");

            migrationBuilder.DropTable(
                name: "FootballFields");

            migrationBuilder.DropIndex(
                name: "IX_TrainingTeams_FootballFieldId",
                table: "TrainingTeams");

            migrationBuilder.DropColumn(
                name: "FootballFieldId",
                table: "TrainingTeams");
        }
    }
}
