using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldCashHandovers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_FieldPayments_ClubId_Id",
                table: "FieldPayments",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.CreateTable(
                name: "FieldCashHandovers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OperatorId = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfirmedById = table.Column<string>(type: "text", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedById = table.Column<string>(type: "text", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ClubId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldCashHandovers", x => x.Id);
                    table.UniqueConstraint("AK_FieldCashHandovers_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_FieldCashHandovers_AspNetUsers_ConfirmedById",
                        column: x => x.ConfirmedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldCashHandovers_AspNetUsers_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldCashHandovers_AspNetUsers_RejectedById",
                        column: x => x.RejectedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldCashHandovers_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FieldCashHandoverItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HandoverId = table.Column<int>(type: "integer", nullable: false),
                    FieldPaymentId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ClubId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldCashHandoverItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldCashHandoverItems_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldCashHandoverItems_FieldCashHandovers_ClubId_HandoverId",
                        columns: x => new { x.ClubId, x.HandoverId },
                        principalTable: "FieldCashHandovers",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldCashHandoverItems_FieldCashHandovers_HandoverId",
                        column: x => x.HandoverId,
                        principalTable: "FieldCashHandovers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FieldCashHandoverItems_FieldPayments_ClubId_FieldPaymentId",
                        columns: x => new { x.ClubId, x.FieldPaymentId },
                        principalTable: "FieldPayments",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldCashHandoverItems_FieldPayments_FieldPaymentId",
                        column: x => x.FieldPaymentId,
                        principalTable: "FieldPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FieldCashHandoverItems_ClubId_FieldPaymentId",
                table: "FieldCashHandoverItems",
                columns: new[] { "ClubId", "FieldPaymentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FieldCashHandoverItems_ClubId_HandoverId",
                table: "FieldCashHandoverItems",
                columns: new[] { "ClubId", "HandoverId" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldCashHandoverItems_FieldPaymentId",
                table: "FieldCashHandoverItems",
                column: "FieldPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldCashHandoverItems_HandoverId",
                table: "FieldCashHandoverItems",
                column: "HandoverId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldCashHandovers_ConfirmedById",
                table: "FieldCashHandovers",
                column: "ConfirmedById");

            migrationBuilder.CreateIndex(
                name: "IX_FieldCashHandovers_OperatorId_Status",
                table: "FieldCashHandovers",
                columns: new[] { "OperatorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldCashHandovers_RejectedById",
                table: "FieldCashHandovers",
                column: "RejectedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FieldCashHandoverItems");

            migrationBuilder.DropTable(
                name: "FieldCashHandovers");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_FieldPayments_ClubId_Id",
                table: "FieldPayments");
        }
    }
}
