using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class FamilyPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FamilyPaymentId",
                table: "Payments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FamilyPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<int>(type: "integer", nullable: false),
                    FamilyName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FamilyPayments_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FamilyPayments_PlayerFamilies_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "PlayerFamilies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_FamilyPaymentId",
                table: "Payments",
                column: "FamilyPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyPayments_CreatedByUserId",
                table: "FamilyPayments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyPayments_FamilyId",
                table: "FamilyPayments",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyPayments_RequestId",
                table: "FamilyPayments",
                column: "RequestId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_FamilyPayments_FamilyPaymentId",
                table: "Payments",
                column: "FamilyPaymentId",
                principalTable: "FamilyPayments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_FamilyPayments_FamilyPaymentId",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "FamilyPayments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_FamilyPaymentId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "FamilyPaymentId",
                table: "Payments");
        }
    }
}
