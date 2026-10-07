using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class TreasuryOpeningAndOwnerWithdrawals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TreasuryEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Owner = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedById = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledById = table.Column<string>(type: "text", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreasuryEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreasuryEntries_AspNetUsers_CancelledById",
                        column: x => x.CancelledById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TreasuryEntries_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TreasuryEntries_CancelledById",
                table: "TreasuryEntries",
                column: "CancelledById");

            migrationBuilder.CreateIndex(
                name: "IX_TreasuryEntries_CreatedById",
                table: "TreasuryEntries",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TreasuryEntries_Date",
                table: "TreasuryEntries",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_TreasuryEntries_Method",
                table: "TreasuryEntries",
                column: "Method",
                unique: true,
                filter: "\"Kind\" = 'Opening' AND \"CancelledAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TreasuryEntries_RequestId",
                table: "TreasuryEntries",
                column: "RequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TreasuryEntries");
        }
    }
}
