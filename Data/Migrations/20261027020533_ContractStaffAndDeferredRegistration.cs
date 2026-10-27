using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class ContractStaffAndDeferredRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ContractPending",
                table: "Students",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "StaffBirthDate",
                table: "SponsorshipBeneficiaries",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StaffCategory",
                table: "SponsorshipBeneficiaries",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StaffTeamId",
                table: "SponsorshipBeneficiaries",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "StudentId",
                table: "PlayerContracts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipBeneficiaries_ClubId_StaffTeamId",
                table: "SponsorshipBeneficiaries",
                columns: new[] { "ClubId", "StaffTeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipBeneficiaries_StaffTeamId",
                table: "SponsorshipBeneficiaries",
                column: "StaffTeamId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PlayerContracts_Person",
                table: "PlayerContracts",
                sql: "\"StudentId\" IS NOT NULL OR \"BeneficiaryId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_SponsorshipBeneficiaries_TrainingTeams_ClubId_StaffTeamId",
                table: "SponsorshipBeneficiaries",
                columns: new[] { "ClubId", "StaffTeamId" },
                principalTable: "TrainingTeams",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SponsorshipBeneficiaries_TrainingTeams_StaffTeamId",
                table: "SponsorshipBeneficiaries",
                column: "StaffTeamId",
                principalTable: "TrainingTeams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SponsorshipBeneficiaries_TrainingTeams_ClubId_StaffTeamId",
                table: "SponsorshipBeneficiaries");

            migrationBuilder.DropForeignKey(
                name: "FK_SponsorshipBeneficiaries_TrainingTeams_StaffTeamId",
                table: "SponsorshipBeneficiaries");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipBeneficiaries_ClubId_StaffTeamId",
                table: "SponsorshipBeneficiaries");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipBeneficiaries_StaffTeamId",
                table: "SponsorshipBeneficiaries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PlayerContracts_Person",
                table: "PlayerContracts");

            migrationBuilder.DropColumn(
                name: "ContractPending",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "StaffBirthDate",
                table: "SponsorshipBeneficiaries");

            migrationBuilder.DropColumn(
                name: "StaffCategory",
                table: "SponsorshipBeneficiaries");

            migrationBuilder.DropColumn(
                name: "StaffTeamId",
                table: "SponsorshipBeneficiaries");

            migrationBuilder.AlterColumn<int>(
                name: "StudentId",
                table: "PlayerContracts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
