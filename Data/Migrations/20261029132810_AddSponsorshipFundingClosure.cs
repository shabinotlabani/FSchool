using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSponsorshipFundingClosure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FundingClosedAt",
                table: "SponsorshipContracts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FundingClosedBy",
                table: "SponsorshipContracts",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FundingClosureReason",
                table: "SponsorshipContracts",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FundingClosedAt",
                table: "SponsorshipContracts");

            migrationBuilder.DropColumn(
                name: "FundingClosedBy",
                table: "SponsorshipContracts");

            migrationBuilder.DropColumn(
                name: "FundingClosureReason",
                table: "SponsorshipContracts");
        }
    }
}
