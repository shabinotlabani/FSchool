using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class MultiClub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TreasuryEntries_Method",
                table: "TreasuryEntries");

            migrationBuilder.DropIndex(
                name: "IX_TeamAttendances_TrainingTeamId_Date",
                table: "TeamAttendances");

            migrationBuilder.DropIndex(
                name: "IX_StockEntries_DocumentNumber",
                table: "StockEntries");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipSpending_BankReference",
                table: "SponsorshipSpending");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipSpending_BeneficiaryId_DocumentReference",
                table: "SponsorshipSpending");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipSpending_BeneficiaryId_SalaryMonth",
                table: "SponsorshipSpending");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipReceipts_BankReference",
                table: "SponsorshipReceipts");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipPlans_Year",
                table: "SponsorshipPlans");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipPlanLines_PlanId_Category",
                table: "SponsorshipPlanLines");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipContracts_SponsorNumber_Reference",
                table: "SponsorshipContracts");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipBeneficiaries_Identifier",
                table: "SponsorshipBeneficiaries");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipAllocations_ContractId_PlanLineId",
                table: "SponsorshipAllocations");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ProductId_Size",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_PersonalCharges_PersonalTrainingId_Period",
                table: "PersonalCharges");

            migrationBuilder.DropIndex(
                name: "IX_ParentPushSubscriptions_StudentId_EndpointHash",
                table: "ParentPushSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_ParentPushDeliveries_MessageId_SubscriptionId",
                table: "ParentPushDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_MonthlyFeePeriods_StudentId_Year_Month",
                table: "MonthlyFeePeriods");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseCategories_ParentId_Name",
                table: "ExpenseCategories");

            migrationBuilder.DropIndex(
                name: "IX_BankPaymentAllocations_PaymentId",
                table: "BankPaymentAllocations");

            migrationBuilder.DropIndex(
                name: "IX_AttendancePlayers_TeamAttendanceId_StudentId",
                table: "AttendancePlayers");







            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "TreasuryEntries",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "TrainingTeams",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "TrainingSessions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "TeamChanges",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "TeamAttendances",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "StudentTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "StudentTeamChanges",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "Students",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "StudentMonthlyExemptions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "StudentFeeAssignments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "StudentChanges",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "StockMovements",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "StockEntryDetails",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "StockEntries",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "SponsorshipSpending",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "SponsorshipReceipts",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "SponsorshipPlans",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "SponsorshipPlanLines",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "SponsorshipContracts",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "SponsorshipBeneficiaries",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "SponsorshipAudits",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "SponsorshipAllocations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "SizeGroups",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "Sales",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "SaleDetails",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "ProductVariants",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "PlayerFamilies",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "PlayerEvaluations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "PersonalTrainingSlots",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "PersonalTrainings",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "PersonalTrainingAudits",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "PersonalTariffs",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "PersonalCharges",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "PendingBankPayments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "Payments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "ParentScheduleStates",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "ParentPushSubscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "ParentPushSettings",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "ParentPushMessages",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "ParentPushDeliveries",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "NutritionMenus",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "MonthlyFeePeriods",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "FootballFields",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "FirstMonthFeeChanges",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "FieldPayments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "FieldBookings",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "FieldBookingChanges",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "FeePlans",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "FeePlanPrices",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "FamilyTariffAssignments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "FamilyRegistrations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "FamilyPayments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "FamilyChanges",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "Expenses",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "ExpenseCategoryChanges",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "ExpenseCategories",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "DevelopmentRecords",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "DemoImports",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "CoachTeams",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "ClubSettings",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "CareReferrals",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "CareEntries",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "CareAssignments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "BankPaymentAllocations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "AttendancePlayers",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "AttendanceChanges",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "IsPlatformOwner",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_TrainingTeams_ClubId_Id",
                table: "TrainingTeams",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_TeamAttendances_ClubId_Id",
                table: "TeamAttendances",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Students_ClubId_Id",
                table: "Students",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_StockEntries_ClubId_Id",
                table: "StockEntries",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_SponsorshipPlans_ClubId_Id",
                table: "SponsorshipPlans",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_SponsorshipPlanLines_ClubId_Id",
                table: "SponsorshipPlanLines",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_SponsorshipContracts_ClubId_Id",
                table: "SponsorshipContracts",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_SponsorshipBeneficiaries_ClubId_Id",
                table: "SponsorshipBeneficiaries",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_SponsorshipAllocations_ClubId_Id",
                table: "SponsorshipAllocations",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Sales_ClubId_Id",
                table: "Sales",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ProductVariants_ClubId_ProductId_Id",
                table: "ProductVariants",
                columns: new[] { "ClubId", "ProductId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Products_ClubId_Id",
                table: "Products",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_PlayerFamilies_ClubId_Id",
                table: "PlayerFamilies",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_PersonalTrainings_ClubId_Id",
                table: "PersonalTrainings",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_PersonalTariffs_ClubId_Id",
                table: "PersonalTariffs",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_PersonalCharges_ClubId_Id",
                table: "PersonalCharges",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_PendingBankPayments_ClubId_Id",
                table: "PendingBankPayments",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Payments_ClubId_Id",
                table: "Payments",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ParentPushSubscriptions_ClubId_Id",
                table: "ParentPushSubscriptions",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ParentPushMessages_ClubId_Id",
                table: "ParentPushMessages",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_MonthlyFeePeriods_ClubId_Id",
                table: "MonthlyFeePeriods",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_FootballFields_ClubId_Id",
                table: "FootballFields",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_FieldBookings_ClubId_Id",
                table: "FieldBookings",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_FeePlans_ClubId_Id",
                table: "FeePlans",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_FamilyPayments_ClubId_Id",
                table: "FamilyPayments",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ExpenseCategories_ClubId_Id",
                table: "ExpenseCategories",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_CareAssignments_ClubId_Id",
                table: "CareAssignments",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.CreateTable(
                name: "Clubs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExpiresOn = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clubs", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Clubs",
                columns: new[] { "Id", "CreatedAt", "ExpiresOn", "IsActive", "Name" },
                values: new object[] { 1, new DateTime(2026, 10, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "2 Korriku" });
            // Keep an already customized club name during the single-club upgrade.
            migrationBuilder.Sql("UPDATE \"Clubs\" c SET \"Name\" = s.\"Name\" FROM \"ClubSettings\" s WHERE c.\"Id\" = 1 AND s.\"Id\" = 1");









            migrationBuilder.CreateIndex(
                name: "IX_TreasuryEntries_ClubId_Method",
                table: "TreasuryEntries",
                columns: new[] { "ClubId", "Method" },
                unique: true,
                filter: "\"Kind\" = 'Opening' AND \"CancelledAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTeams_ClubId_FootballFieldId",
                table: "TrainingTeams",
                columns: new[] { "ClubId", "FootballFieldId" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessions_ClubId_TrainingTeamId",
                table: "TrainingSessions",
                columns: new[] { "ClubId", "TrainingTeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_TeamChanges_ClubId_TrainingTeamId",
                table: "TeamChanges",
                columns: new[] { "ClubId", "TrainingTeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_TeamAttendances_ClubId_TrainingTeamId_Date",
                table: "TeamAttendances",
                columns: new[] { "ClubId", "TrainingTeamId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamAttendances_TrainingTeamId",
                table: "TeamAttendances",
                column: "TrainingTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentTransactions_ClubId_StudentId",
                table: "StudentTransactions",
                columns: new[] { "ClubId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentTeamChanges_ClubId_StudentId",
                table: "StudentTeamChanges",
                columns: new[] { "ClubId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Students_ClubId_TrainingTeamId",
                table: "Students",
                columns: new[] { "ClubId", "TrainingTeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentMonthlyExemptions_ClubId_StudentId",
                table: "StudentMonthlyExemptions",
                columns: new[] { "ClubId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeeAssignments_ClubId_FamilyId",
                table: "StudentFeeAssignments",
                columns: new[] { "ClubId", "FamilyId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeeAssignments_ClubId_FeePlanId",
                table: "StudentFeeAssignments",
                columns: new[] { "ClubId", "FeePlanId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeeAssignments_ClubId_StudentId",
                table: "StudentFeeAssignments",
                columns: new[] { "ClubId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentChanges_ClubId_StudentId",
                table: "StudentChanges",
                columns: new[] { "ClubId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ClubId_ProductId_ProductVariantId",
                table: "StockMovements",
                columns: new[] { "ClubId", "ProductId", "ProductVariantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockEntryDetails_ClubId_ProductId_ProductVariantId",
                table: "StockEntryDetails",
                columns: new[] { "ClubId", "ProductId", "ProductVariantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockEntryDetails_ClubId_StockEntryId",
                table: "StockEntryDetails",
                columns: new[] { "ClubId", "StockEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockEntries_ClubId_DocumentNumber",
                table: "StockEntries",
                columns: new[] { "ClubId", "DocumentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipSpending_BeneficiaryId",
                table: "SponsorshipSpending",
                column: "BeneficiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipSpending_ClubId_AllocationId",
                table: "SponsorshipSpending",
                columns: new[] { "ClubId", "AllocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipSpending_ClubId_BankReference",
                table: "SponsorshipSpending",
                columns: new[] { "ClubId", "BankReference" },
                unique: true,
                filter: "\"CancelledAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipSpending_ClubId_BeneficiaryId_DocumentReference",
                table: "SponsorshipSpending",
                columns: new[] { "ClubId", "BeneficiaryId", "DocumentReference" },
                unique: true,
                filter: "\"CancelledAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipSpending_ClubId_BeneficiaryId_SalaryMonth",
                table: "SponsorshipSpending",
                columns: new[] { "ClubId", "BeneficiaryId", "SalaryMonth" },
                unique: true,
                filter: "\"CancelledAt\" IS NULL AND \"SalaryMonth\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipReceipts_ClubId_BankReference",
                table: "SponsorshipReceipts",
                columns: new[] { "ClubId", "BankReference" },
                unique: true,
                filter: "\"CancelledAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipReceipts_ClubId_ContractId",
                table: "SponsorshipReceipts",
                columns: new[] { "ClubId", "ContractId" });

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipPlans_ClubId_Year",
                table: "SponsorshipPlans",
                columns: new[] { "ClubId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipPlanLines_ClubId_PlanId_Category",
                table: "SponsorshipPlanLines",
                columns: new[] { "ClubId", "PlanId", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipPlanLines_PlanId",
                table: "SponsorshipPlanLines",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipContracts_ClubId_PlanId",
                table: "SponsorshipContracts",
                columns: new[] { "ClubId", "PlanId" });

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipContracts_ClubId_SponsorNumber_Reference",
                table: "SponsorshipContracts",
                columns: new[] { "ClubId", "SponsorNumber", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipBeneficiaries_ClubId_Identifier",
                table: "SponsorshipBeneficiaries",
                columns: new[] { "ClubId", "Identifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipAudits_ClubId",
                table: "SponsorshipAudits",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipAllocations_ClubId_ContractId_PlanLineId",
                table: "SponsorshipAllocations",
                columns: new[] { "ClubId", "ContractId", "PlanLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipAllocations_ClubId_PlanLineId",
                table: "SponsorshipAllocations",
                columns: new[] { "ClubId", "PlanLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipAllocations_ContractId",
                table: "SponsorshipAllocations",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_SizeGroups_ClubId",
                table: "SizeGroups",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_ClubId_StudentId",
                table: "Sales",
                columns: new[] { "ClubId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleDetails_ClubId_ProductId_ProductVariantId",
                table: "SaleDetails",
                columns: new[] { "ClubId", "ProductId", "ProductVariantId" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleDetails_ClubId_SaleId",
                table: "SaleDetails",
                columns: new[] { "ClubId", "SaleId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ClubId_ProductId_Size",
                table: "ProductVariants",
                columns: new[] { "ClubId", "ProductId", "Size" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerEvaluations_ClubId_StudentId",
                table: "PlayerEvaluations",
                columns: new[] { "ClubId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTrainingSlots_ClubId_PersonalTrainingId",
                table: "PersonalTrainingSlots",
                columns: new[] { "ClubId", "PersonalTrainingId" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTrainings_ClubId_PersonalTariffId",
                table: "PersonalTrainings",
                columns: new[] { "ClubId", "PersonalTariffId" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTrainings_ClubId_StudentId",
                table: "PersonalTrainings",
                columns: new[] { "ClubId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTrainingAudits_ClubId_PersonalTariffId",
                table: "PersonalTrainingAudits",
                columns: new[] { "ClubId", "PersonalTariffId" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTrainingAudits_ClubId_PersonalTrainingId",
                table: "PersonalTrainingAudits",
                columns: new[] { "ClubId", "PersonalTrainingId" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalCharges_ClubId_PersonalTrainingId_Period",
                table: "PersonalCharges",
                columns: new[] { "ClubId", "PersonalTrainingId", "Period" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonalCharges_PersonalTrainingId",
                table: "PersonalCharges",
                column: "PersonalTrainingId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingBankPayments_ClubId_PaymentId",
                table: "PendingBankPayments",
                columns: new[] { "ClubId", "PaymentId" });

            migrationBuilder.CreateIndex(
                name: "IX_PendingBankPayments_ClubId_StudentId",
                table: "PendingBankPayments",
                columns: new[] { "ClubId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ClubId_FamilyPaymentId",
                table: "Payments",
                columns: new[] { "ClubId", "FamilyPaymentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ClubId_PersonalChargeId",
                table: "Payments",
                columns: new[] { "ClubId", "PersonalChargeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ClubId_SaleId",
                table: "Payments",
                columns: new[] { "ClubId", "SaleId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ClubId_StudentId",
                table: "Payments",
                columns: new[] { "ClubId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_ParentScheduleStates_ClubId",
                table: "ParentScheduleStates",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentPushSubscriptions_ClubId_StudentId_EndpointHash",
                table: "ParentPushSubscriptions",
                columns: new[] { "ClubId", "StudentId", "EndpointHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParentPushSubscriptions_StudentId",
                table: "ParentPushSubscriptions",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentPushSettings_ClubId",
                table: "ParentPushSettings",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentPushDeliveries_ClubId_MessageId_SubscriptionId",
                table: "ParentPushDeliveries",
                columns: new[] { "ClubId", "MessageId", "SubscriptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParentPushDeliveries_ClubId_SubscriptionId",
                table: "ParentPushDeliveries",
                columns: new[] { "ClubId", "SubscriptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ParentPushDeliveries_MessageId",
                table: "ParentPushDeliveries",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_NutritionMenus_ClubId_Scope",
                table: "NutritionMenus",
                columns: new[] { "ClubId", "Scope" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyFeePeriods_ClubId_StudentId_Year_Month",
                table: "MonthlyFeePeriods",
                columns: new[] { "ClubId", "StudentId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FirstMonthFeeChanges_ClubId_PeriodId",
                table: "FirstMonthFeeChanges",
                columns: new[] { "ClubId", "PeriodId" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldPayments_ClubId_FieldBookingId",
                table: "FieldPayments",
                columns: new[] { "ClubId", "FieldBookingId" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldBookings_ClubId_FootballFieldId",
                table: "FieldBookings",
                columns: new[] { "ClubId", "FootballFieldId" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldBookingChanges_ClubId_FieldBookingId",
                table: "FieldBookingChanges",
                columns: new[] { "ClubId", "FieldBookingId" });

            migrationBuilder.CreateIndex(
                name: "IX_FeePlanPrices_ClubId_FeePlanId",
                table: "FeePlanPrices",
                columns: new[] { "ClubId", "FeePlanId" });

            migrationBuilder.CreateIndex(
                name: "IX_FamilyTariffAssignments_ClubId_FamilyId",
                table: "FamilyTariffAssignments",
                columns: new[] { "ClubId", "FamilyId" });

            migrationBuilder.CreateIndex(
                name: "IX_FamilyTariffAssignments_ClubId_FeePlanId",
                table: "FamilyTariffAssignments",
                columns: new[] { "ClubId", "FeePlanId" });

            migrationBuilder.CreateIndex(
                name: "IX_FamilyRegistrations_ClubId_FamilyId",
                table: "FamilyRegistrations",
                columns: new[] { "ClubId", "FamilyId" });

            migrationBuilder.CreateIndex(
                name: "IX_FamilyPayments_ClubId_FamilyId",
                table: "FamilyPayments",
                columns: new[] { "ClubId", "FamilyId" });

            migrationBuilder.CreateIndex(
                name: "IX_FamilyChanges_ClubId_FamilyId",
                table: "FamilyChanges",
                columns: new[] { "ClubId", "FamilyId" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ClubId_CategoryId",
                table: "Expenses",
                columns: new[] { "ClubId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ClubId_SubcategoryId",
                table: "Expenses",
                columns: new[] { "ClubId", "SubcategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategoryChanges_ClubId_ExpenseCategoryId",
                table: "ExpenseCategoryChanges",
                columns: new[] { "ClubId", "ExpenseCategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_ClubId_ParentId_Name",
                table: "ExpenseCategories",
                columns: new[] { "ClubId", "ParentId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_ParentId",
                table: "ExpenseCategories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_DevelopmentRecords_ClubId_StudentId",
                table: "DevelopmentRecords",
                columns: new[] { "ClubId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_DemoImports_ClubId",
                table: "DemoImports",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachTeams_ClubId_TrainingTeamId",
                table: "CoachTeams",
                columns: new[] { "ClubId", "TrainingTeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClubSettings_ClubId",
                table: "ClubSettings",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_CareReferrals_ClubId_StudentId",
                table: "CareReferrals",
                columns: new[] { "ClubId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_CareEntries_ClubId_AssignmentId",
                table: "CareEntries",
                columns: new[] { "ClubId", "AssignmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_CareAssignments_ClubId_StaffId_StudentId_Kind",
                table: "CareAssignments",
                columns: new[] { "ClubId", "StaffId", "StudentId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareAssignments_ClubId_StudentId",
                table: "CareAssignments",
                columns: new[] { "ClubId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_BankPaymentAllocations_ClubId_BankPaymentId",
                table: "BankPaymentAllocations",
                columns: new[] { "ClubId", "BankPaymentId" });

            migrationBuilder.CreateIndex(
                name: "IX_BankPaymentAllocations_ClubId_PaymentId",
                table: "BankPaymentAllocations",
                columns: new[] { "ClubId", "PaymentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankPaymentAllocations_PaymentId",
                table: "BankPaymentAllocations",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendancePlayers_ClubId_StudentId",
                table: "AttendancePlayers",
                columns: new[] { "ClubId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendancePlayers_ClubId_TeamAttendanceId_StudentId",
                table: "AttendancePlayers",
                columns: new[] { "ClubId", "TeamAttendanceId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendancePlayers_TeamAttendanceId",
                table: "AttendancePlayers",
                column: "TeamAttendanceId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceChanges_ClubId_TeamAttendanceId",
                table: "AttendanceChanges",
                columns: new[] { "ClubId", "TeamAttendanceId" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ClubId",
                table: "AspNetUsers",
                column: "ClubId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Clubs_ClubId",
                table: "AspNetUsers",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceChanges_Clubs_ClubId",
                table: "AttendanceChanges",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceChanges_TeamAttendances_ClubId_TeamAttendanceId",
                table: "AttendanceChanges",
                columns: new[] { "ClubId", "TeamAttendanceId" },
                principalTable: "TeamAttendances",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendancePlayers_Clubs_ClubId",
                table: "AttendancePlayers",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendancePlayers_Students_ClubId_StudentId",
                table: "AttendancePlayers",
                columns: new[] { "ClubId", "StudentId" },
                principalTable: "Students",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendancePlayers_TeamAttendances_ClubId_TeamAttendanceId",
                table: "AttendancePlayers",
                columns: new[] { "ClubId", "TeamAttendanceId" },
                principalTable: "TeamAttendances",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BankPaymentAllocations_Clubs_ClubId",
                table: "BankPaymentAllocations",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BankPaymentAllocations_Payments_ClubId_PaymentId",
                table: "BankPaymentAllocations",
                columns: new[] { "ClubId", "PaymentId" },
                principalTable: "Payments",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BankPaymentAllocations_PendingBankPayments_ClubId_BankPayme~",
                table: "BankPaymentAllocations",
                columns: new[] { "ClubId", "BankPaymentId" },
                principalTable: "PendingBankPayments",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CareAssignments_Clubs_ClubId",
                table: "CareAssignments",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CareAssignments_Students_ClubId_StudentId",
                table: "CareAssignments",
                columns: new[] { "ClubId", "StudentId" },
                principalTable: "Students",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CareEntries_CareAssignments_ClubId_AssignmentId",
                table: "CareEntries",
                columns: new[] { "ClubId", "AssignmentId" },
                principalTable: "CareAssignments",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CareEntries_Clubs_ClubId",
                table: "CareEntries",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CareReferrals_Clubs_ClubId",
                table: "CareReferrals",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CareReferrals_Students_ClubId_StudentId",
                table: "CareReferrals",
                columns: new[] { "ClubId", "StudentId" },
                principalTable: "Students",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClubSettings_Clubs_ClubId",
                table: "ClubSettings",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CoachTeams_Clubs_ClubId",
                table: "CoachTeams",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CoachTeams_TrainingTeams_ClubId_TrainingTeamId",
                table: "CoachTeams",
                columns: new[] { "ClubId", "TrainingTeamId" },
                principalTable: "TrainingTeams",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DemoImports_Clubs_ClubId",
                table: "DemoImports",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DevelopmentRecords_Clubs_ClubId",
                table: "DevelopmentRecords",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DevelopmentRecords_Students_ClubId_StudentId",
                table: "DevelopmentRecords",
                columns: new[] { "ClubId", "StudentId" },
                principalTable: "Students",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseCategories_Clubs_ClubId",
                table: "ExpenseCategories",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseCategories_ExpenseCategories_ClubId_ParentId",
                table: "ExpenseCategories",
                columns: new[] { "ClubId", "ParentId" },
                principalTable: "ExpenseCategories",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseCategoryChanges_Clubs_ClubId",
                table: "ExpenseCategoryChanges",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseCategoryChanges_ExpenseCategories_ClubId_ExpenseCate~",
                table: "ExpenseCategoryChanges",
                columns: new[] { "ClubId", "ExpenseCategoryId" },
                principalTable: "ExpenseCategories",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Clubs_ClubId",
                table: "Expenses",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_ExpenseCategories_ClubId_CategoryId",
                table: "Expenses",
                columns: new[] { "ClubId", "CategoryId" },
                principalTable: "ExpenseCategories",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_ExpenseCategories_ClubId_SubcategoryId",
                table: "Expenses",
                columns: new[] { "ClubId", "SubcategoryId" },
                principalTable: "ExpenseCategories",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FamilyChanges_Clubs_ClubId",
                table: "FamilyChanges",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FamilyChanges_PlayerFamilies_ClubId_FamilyId",
                table: "FamilyChanges",
                columns: new[] { "ClubId", "FamilyId" },
                principalTable: "PlayerFamilies",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FamilyPayments_Clubs_ClubId",
                table: "FamilyPayments",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FamilyPayments_PlayerFamilies_ClubId_FamilyId",
                table: "FamilyPayments",
                columns: new[] { "ClubId", "FamilyId" },
                principalTable: "PlayerFamilies",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FamilyRegistrations_Clubs_ClubId",
                table: "FamilyRegistrations",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FamilyRegistrations_PlayerFamilies_ClubId_FamilyId",
                table: "FamilyRegistrations",
                columns: new[] { "ClubId", "FamilyId" },
                principalTable: "PlayerFamilies",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FamilyTariffAssignments_Clubs_ClubId",
                table: "FamilyTariffAssignments",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FamilyTariffAssignments_FeePlans_ClubId_FeePlanId",
                table: "FamilyTariffAssignments",
                columns: new[] { "ClubId", "FeePlanId" },
                principalTable: "FeePlans",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FamilyTariffAssignments_PlayerFamilies_ClubId_FamilyId",
                table: "FamilyTariffAssignments",
                columns: new[] { "ClubId", "FamilyId" },
                principalTable: "PlayerFamilies",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FeePlanPrices_Clubs_ClubId",
                table: "FeePlanPrices",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FeePlanPrices_FeePlans_ClubId_FeePlanId",
                table: "FeePlanPrices",
                columns: new[] { "ClubId", "FeePlanId" },
                principalTable: "FeePlans",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FeePlans_Clubs_ClubId",
                table: "FeePlans",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldBookingChanges_Clubs_ClubId",
                table: "FieldBookingChanges",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldBookingChanges_FieldBookings_ClubId_FieldBookingId",
                table: "FieldBookingChanges",
                columns: new[] { "ClubId", "FieldBookingId" },
                principalTable: "FieldBookings",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldBookings_Clubs_ClubId",
                table: "FieldBookings",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldBookings_FootballFields_ClubId_FootballFieldId",
                table: "FieldBookings",
                columns: new[] { "ClubId", "FootballFieldId" },
                principalTable: "FootballFields",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldPayments_Clubs_ClubId",
                table: "FieldPayments",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldPayments_FieldBookings_ClubId_FieldBookingId",
                table: "FieldPayments",
                columns: new[] { "ClubId", "FieldBookingId" },
                principalTable: "FieldBookings",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FirstMonthFeeChanges_Clubs_ClubId",
                table: "FirstMonthFeeChanges",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FirstMonthFeeChanges_MonthlyFeePeriods_ClubId_PeriodId",
                table: "FirstMonthFeeChanges",
                columns: new[] { "ClubId", "PeriodId" },
                principalTable: "MonthlyFeePeriods",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FootballFields_Clubs_ClubId",
                table: "FootballFields",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MonthlyFeePeriods_Clubs_ClubId",
                table: "MonthlyFeePeriods",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MonthlyFeePeriods_Students_ClubId_StudentId",
                table: "MonthlyFeePeriods",
                columns: new[] { "ClubId", "StudentId" },
                principalTable: "Students",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NutritionMenus_Clubs_ClubId",
                table: "NutritionMenus",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ParentPushDeliveries_Clubs_ClubId",
                table: "ParentPushDeliveries",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ParentPushDeliveries_ParentPushMessages_ClubId_MessageId",
                table: "ParentPushDeliveries",
                columns: new[] { "ClubId", "MessageId" },
                principalTable: "ParentPushMessages",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ParentPushDeliveries_ParentPushSubscriptions_ClubId_Subscri~",
                table: "ParentPushDeliveries",
                columns: new[] { "ClubId", "SubscriptionId" },
                principalTable: "ParentPushSubscriptions",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ParentPushMessages_Clubs_ClubId",
                table: "ParentPushMessages",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ParentPushSettings_Clubs_ClubId",
                table: "ParentPushSettings",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ParentPushSubscriptions_Clubs_ClubId",
                table: "ParentPushSubscriptions",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ParentPushSubscriptions_Students_ClubId_StudentId",
                table: "ParentPushSubscriptions",
                columns: new[] { "ClubId", "StudentId" },
                principalTable: "Students",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ParentScheduleStates_Clubs_ClubId",
                table: "ParentScheduleStates",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Clubs_ClubId",
                table: "Payments",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_FamilyPayments_ClubId_FamilyPaymentId",
                table: "Payments",
                columns: new[] { "ClubId", "FamilyPaymentId" },
                principalTable: "FamilyPayments",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_PersonalCharges_ClubId_PersonalChargeId",
                table: "Payments",
                columns: new[] { "ClubId", "PersonalChargeId" },
                principalTable: "PersonalCharges",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Sales_ClubId_SaleId",
                table: "Payments",
                columns: new[] { "ClubId", "SaleId" },
                principalTable: "Sales",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Students_ClubId_StudentId",
                table: "Payments",
                columns: new[] { "ClubId", "StudentId" },
                principalTable: "Students",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PendingBankPayments_Clubs_ClubId",
                table: "PendingBankPayments",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PendingBankPayments_Payments_ClubId_PaymentId",
                table: "PendingBankPayments",
                columns: new[] { "ClubId", "PaymentId" },
                principalTable: "Payments",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PendingBankPayments_Students_ClubId_StudentId",
                table: "PendingBankPayments",
                columns: new[] { "ClubId", "StudentId" },
                principalTable: "Students",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonalCharges_Clubs_ClubId",
                table: "PersonalCharges",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonalCharges_PersonalTrainings_ClubId_PersonalTrainingId",
                table: "PersonalCharges",
                columns: new[] { "ClubId", "PersonalTrainingId" },
                principalTable: "PersonalTrainings",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonalTariffs_Clubs_ClubId",
                table: "PersonalTariffs",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonalTrainingAudits_Clubs_ClubId",
                table: "PersonalTrainingAudits",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonalTrainingAudits_PersonalTariffs_ClubId_PersonalTarif~",
                table: "PersonalTrainingAudits",
                columns: new[] { "ClubId", "PersonalTariffId" },
                principalTable: "PersonalTariffs",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonalTrainingAudits_PersonalTrainings_ClubId_PersonalTra~",
                table: "PersonalTrainingAudits",
                columns: new[] { "ClubId", "PersonalTrainingId" },
                principalTable: "PersonalTrainings",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonalTrainings_Clubs_ClubId",
                table: "PersonalTrainings",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonalTrainings_PersonalTariffs_ClubId_PersonalTariffId",
                table: "PersonalTrainings",
                columns: new[] { "ClubId", "PersonalTariffId" },
                principalTable: "PersonalTariffs",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonalTrainings_Students_ClubId_StudentId",
                table: "PersonalTrainings",
                columns: new[] { "ClubId", "StudentId" },
                principalTable: "Students",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonalTrainingSlots_Clubs_ClubId",
                table: "PersonalTrainingSlots",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonalTrainingSlots_PersonalTrainings_ClubId_PersonalTrai~",
                table: "PersonalTrainingSlots",
                columns: new[] { "ClubId", "PersonalTrainingId" },
                principalTable: "PersonalTrainings",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerEvaluations_Clubs_ClubId",
                table: "PlayerEvaluations",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerEvaluations_Students_ClubId_StudentId",
                table: "PlayerEvaluations",
                columns: new[] { "ClubId", "StudentId" },
                principalTable: "Students",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerFamilies_Clubs_ClubId",
                table: "PlayerFamilies",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Clubs_ClubId",
                table: "Products",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariants_Clubs_ClubId",
                table: "ProductVariants",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariants_Products_ClubId_ProductId",
                table: "ProductVariants",
                columns: new[] { "ClubId", "ProductId" },
                principalTable: "Products",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleDetails_Clubs_ClubId",
                table: "SaleDetails",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleDetails_ProductVariants_ClubId_ProductId_ProductVariant~",
                table: "SaleDetails",
                columns: new[] { "ClubId", "ProductId", "ProductVariantId" },
                principalTable: "ProductVariants",
                principalColumns: new[] { "ClubId", "ProductId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleDetails_Products_ClubId_ProductId",
                table: "SaleDetails",
                columns: new[] { "ClubId", "ProductId" },
                principalTable: "Products",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleDetails_Sales_ClubId_SaleId",
                table: "SaleDetails",
                columns: new[] { "ClubId", "SaleId" },
                principalTable: "Sales",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Clubs_ClubId",
                table: "Sales",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Students_ClubId_StudentId",
                table: "Sales",
                columns: new[] { "ClubId", "StudentId" },
                principalTable: "Students",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SizeGroups_Clubs_ClubId",
                table: "SizeGroups",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SponsorshipAllocations_Clubs_ClubId",
                table: "SponsorshipAllocations",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SponsorshipAllocations_SponsorshipContracts_ClubId_Contract~",
                table: "SponsorshipAllocations",
                columns: new[] { "ClubId", "ContractId" },
                principalTable: "SponsorshipContracts",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SponsorshipAllocations_SponsorshipPlanLines_ClubId_PlanLine~",
                table: "SponsorshipAllocations",
                columns: new[] { "ClubId", "PlanLineId" },
                principalTable: "SponsorshipPlanLines",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SponsorshipAudits_Clubs_ClubId",
                table: "SponsorshipAudits",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SponsorshipBeneficiaries_Clubs_ClubId",
                table: "SponsorshipBeneficiaries",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SponsorshipContracts_Clubs_ClubId",
                table: "SponsorshipContracts",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SponsorshipContracts_SponsorshipPlans_ClubId_PlanId",
                table: "SponsorshipContracts",
                columns: new[] { "ClubId", "PlanId" },
                principalTable: "SponsorshipPlans",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SponsorshipPlanLines_Clubs_ClubId",
                table: "SponsorshipPlanLines",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SponsorshipPlanLines_SponsorshipPlans_ClubId_PlanId",
                table: "SponsorshipPlanLines",
                columns: new[] { "ClubId", "PlanId" },
                principalTable: "SponsorshipPlans",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SponsorshipPlans_Clubs_ClubId",
                table: "SponsorshipPlans",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SponsorshipReceipts_Clubs_ClubId",
                table: "SponsorshipReceipts",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SponsorshipReceipts_SponsorshipContracts_ClubId_ContractId",
                table: "SponsorshipReceipts",
                columns: new[] { "ClubId", "ContractId" },
                principalTable: "SponsorshipContracts",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SponsorshipSpending_Clubs_ClubId",
                table: "SponsorshipSpending",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SponsorshipSpending_SponsorshipAllocations_ClubId_Allocatio~",
                table: "SponsorshipSpending",
                columns: new[] { "ClubId", "AllocationId" },
                principalTable: "SponsorshipAllocations",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SponsorshipSpending_SponsorshipBeneficiaries_ClubId_Benefic~",
                table: "SponsorshipSpending",
                columns: new[] { "ClubId", "BeneficiaryId" },
                principalTable: "SponsorshipBeneficiaries",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockEntries_Clubs_ClubId",
                table: "StockEntries",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockEntryDetails_Clubs_ClubId",
                table: "StockEntryDetails",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockEntryDetails_ProductVariants_ClubId_ProductId_ProductV~",
                table: "StockEntryDetails",
                columns: new[] { "ClubId", "ProductId", "ProductVariantId" },
                principalTable: "ProductVariants",
                principalColumns: new[] { "ClubId", "ProductId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockEntryDetails_Products_ClubId_ProductId",
                table: "StockEntryDetails",
                columns: new[] { "ClubId", "ProductId" },
                principalTable: "Products",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockEntryDetails_StockEntries_ClubId_StockEntryId",
                table: "StockEntryDetails",
                columns: new[] { "ClubId", "StockEntryId" },
                principalTable: "StockEntries",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Clubs_ClubId",
                table: "StockMovements",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_ProductVariants_ClubId_ProductId_ProductVari~",
                table: "StockMovements",
                columns: new[] { "ClubId", "ProductId", "ProductVariantId" },
                principalTable: "ProductVariants",
                principalColumns: new[] { "ClubId", "ProductId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Products_ClubId_ProductId",
                table: "StockMovements",
                columns: new[] { "ClubId", "ProductId" },
                principalTable: "Products",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentChanges_Clubs_ClubId",
                table: "StudentChanges",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentChanges_Students_ClubId_StudentId",
                table: "StudentChanges",
                columns: new[] { "ClubId", "StudentId" },
                principalTable: "Students",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentFeeAssignments_Clubs_ClubId",
                table: "StudentFeeAssignments",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentFeeAssignments_FeePlans_ClubId_FeePlanId",
                table: "StudentFeeAssignments",
                columns: new[] { "ClubId", "FeePlanId" },
                principalTable: "FeePlans",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentFeeAssignments_PlayerFamilies_ClubId_FamilyId",
                table: "StudentFeeAssignments",
                columns: new[] { "ClubId", "FamilyId" },
                principalTable: "PlayerFamilies",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentFeeAssignments_Students_ClubId_StudentId",
                table: "StudentFeeAssignments",
                columns: new[] { "ClubId", "StudentId" },
                principalTable: "Students",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentMonthlyExemptions_Clubs_ClubId",
                table: "StudentMonthlyExemptions",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentMonthlyExemptions_Students_ClubId_StudentId",
                table: "StudentMonthlyExemptions",
                columns: new[] { "ClubId", "StudentId" },
                principalTable: "Students",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Clubs_ClubId",
                table: "Students",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_TrainingTeams_ClubId_TrainingTeamId",
                table: "Students",
                columns: new[] { "ClubId", "TrainingTeamId" },
                principalTable: "TrainingTeams",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentTeamChanges_Clubs_ClubId",
                table: "StudentTeamChanges",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentTeamChanges_Students_ClubId_StudentId",
                table: "StudentTeamChanges",
                columns: new[] { "ClubId", "StudentId" },
                principalTable: "Students",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentTransactions_Clubs_ClubId",
                table: "StudentTransactions",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentTransactions_Students_ClubId_StudentId",
                table: "StudentTransactions",
                columns: new[] { "ClubId", "StudentId" },
                principalTable: "Students",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamAttendances_Clubs_ClubId",
                table: "TeamAttendances",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamAttendances_TrainingTeams_ClubId_TrainingTeamId",
                table: "TeamAttendances",
                columns: new[] { "ClubId", "TrainingTeamId" },
                principalTable: "TrainingTeams",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamChanges_Clubs_ClubId",
                table: "TeamChanges",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamChanges_TrainingTeams_ClubId_TrainingTeamId",
                table: "TeamChanges",
                columns: new[] { "ClubId", "TrainingTeamId" },
                principalTable: "TrainingTeams",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingSessions_Clubs_ClubId",
                table: "TrainingSessions",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingSessions_TrainingTeams_ClubId_TrainingTeamId",
                table: "TrainingSessions",
                columns: new[] { "ClubId", "TrainingTeamId" },
                principalTable: "TrainingTeams",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingTeams_Clubs_ClubId",
                table: "TrainingTeams",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingTeams_FootballFields_ClubId_FootballFieldId",
                table: "TrainingTeams",
                columns: new[] { "ClubId", "FootballFieldId" },
                principalTable: "FootballFields",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TreasuryEntries_Clubs_ClubId",
                table: "TreasuryEntries",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS (SELECT 1 FROM \"Clubs\" WHERE \"Id\" <> 1) THEN RAISE EXCEPTION 'MultiClub rollback requires removal of additional clubs through a reviewed data migration'; END IF; END $$;");
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Clubs_ClubId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceChanges_Clubs_ClubId",
                table: "AttendanceChanges");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceChanges_TeamAttendances_ClubId_TeamAttendanceId",
                table: "AttendanceChanges");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendancePlayers_Clubs_ClubId",
                table: "AttendancePlayers");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendancePlayers_Students_ClubId_StudentId",
                table: "AttendancePlayers");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendancePlayers_TeamAttendances_ClubId_TeamAttendanceId",
                table: "AttendancePlayers");

            migrationBuilder.DropForeignKey(
                name: "FK_BankPaymentAllocations_Clubs_ClubId",
                table: "BankPaymentAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_BankPaymentAllocations_Payments_ClubId_PaymentId",
                table: "BankPaymentAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_BankPaymentAllocations_PendingBankPayments_ClubId_BankPayme~",
                table: "BankPaymentAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_CareAssignments_Clubs_ClubId",
                table: "CareAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_CareAssignments_Students_ClubId_StudentId",
                table: "CareAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_CareEntries_CareAssignments_ClubId_AssignmentId",
                table: "CareEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_CareEntries_Clubs_ClubId",
                table: "CareEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_CareReferrals_Clubs_ClubId",
                table: "CareReferrals");

            migrationBuilder.DropForeignKey(
                name: "FK_CareReferrals_Students_ClubId_StudentId",
                table: "CareReferrals");

            migrationBuilder.DropForeignKey(
                name: "FK_ClubSettings_Clubs_ClubId",
                table: "ClubSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_CoachTeams_Clubs_ClubId",
                table: "CoachTeams");

            migrationBuilder.DropForeignKey(
                name: "FK_CoachTeams_TrainingTeams_ClubId_TrainingTeamId",
                table: "CoachTeams");

            migrationBuilder.DropForeignKey(
                name: "FK_DemoImports_Clubs_ClubId",
                table: "DemoImports");

            migrationBuilder.DropForeignKey(
                name: "FK_DevelopmentRecords_Clubs_ClubId",
                table: "DevelopmentRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_DevelopmentRecords_Students_ClubId_StudentId",
                table: "DevelopmentRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseCategories_Clubs_ClubId",
                table: "ExpenseCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseCategories_ExpenseCategories_ClubId_ParentId",
                table: "ExpenseCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseCategoryChanges_Clubs_ClubId",
                table: "ExpenseCategoryChanges");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseCategoryChanges_ExpenseCategories_ClubId_ExpenseCate~",
                table: "ExpenseCategoryChanges");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Clubs_ClubId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_ExpenseCategories_ClubId_CategoryId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_ExpenseCategories_ClubId_SubcategoryId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_FamilyChanges_Clubs_ClubId",
                table: "FamilyChanges");

            migrationBuilder.DropForeignKey(
                name: "FK_FamilyChanges_PlayerFamilies_ClubId_FamilyId",
                table: "FamilyChanges");

            migrationBuilder.DropForeignKey(
                name: "FK_FamilyPayments_Clubs_ClubId",
                table: "FamilyPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_FamilyPayments_PlayerFamilies_ClubId_FamilyId",
                table: "FamilyPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_FamilyRegistrations_Clubs_ClubId",
                table: "FamilyRegistrations");

            migrationBuilder.DropForeignKey(
                name: "FK_FamilyRegistrations_PlayerFamilies_ClubId_FamilyId",
                table: "FamilyRegistrations");

            migrationBuilder.DropForeignKey(
                name: "FK_FamilyTariffAssignments_Clubs_ClubId",
                table: "FamilyTariffAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_FamilyTariffAssignments_FeePlans_ClubId_FeePlanId",
                table: "FamilyTariffAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_FamilyTariffAssignments_PlayerFamilies_ClubId_FamilyId",
                table: "FamilyTariffAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_FeePlanPrices_Clubs_ClubId",
                table: "FeePlanPrices");

            migrationBuilder.DropForeignKey(
                name: "FK_FeePlanPrices_FeePlans_ClubId_FeePlanId",
                table: "FeePlanPrices");

            migrationBuilder.DropForeignKey(
                name: "FK_FeePlans_Clubs_ClubId",
                table: "FeePlans");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldBookingChanges_Clubs_ClubId",
                table: "FieldBookingChanges");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldBookingChanges_FieldBookings_ClubId_FieldBookingId",
                table: "FieldBookingChanges");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldBookings_Clubs_ClubId",
                table: "FieldBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldBookings_FootballFields_ClubId_FootballFieldId",
                table: "FieldBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldPayments_Clubs_ClubId",
                table: "FieldPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldPayments_FieldBookings_ClubId_FieldBookingId",
                table: "FieldPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_FirstMonthFeeChanges_Clubs_ClubId",
                table: "FirstMonthFeeChanges");

            migrationBuilder.DropForeignKey(
                name: "FK_FirstMonthFeeChanges_MonthlyFeePeriods_ClubId_PeriodId",
                table: "FirstMonthFeeChanges");

            migrationBuilder.DropForeignKey(
                name: "FK_FootballFields_Clubs_ClubId",
                table: "FootballFields");

            migrationBuilder.DropForeignKey(
                name: "FK_MonthlyFeePeriods_Clubs_ClubId",
                table: "MonthlyFeePeriods");

            migrationBuilder.DropForeignKey(
                name: "FK_MonthlyFeePeriods_Students_ClubId_StudentId",
                table: "MonthlyFeePeriods");

            migrationBuilder.DropForeignKey(
                name: "FK_NutritionMenus_Clubs_ClubId",
                table: "NutritionMenus");

            migrationBuilder.DropForeignKey(
                name: "FK_ParentPushDeliveries_Clubs_ClubId",
                table: "ParentPushDeliveries");

            migrationBuilder.DropForeignKey(
                name: "FK_ParentPushDeliveries_ParentPushMessages_ClubId_MessageId",
                table: "ParentPushDeliveries");

            migrationBuilder.DropForeignKey(
                name: "FK_ParentPushDeliveries_ParentPushSubscriptions_ClubId_Subscri~",
                table: "ParentPushDeliveries");

            migrationBuilder.DropForeignKey(
                name: "FK_ParentPushMessages_Clubs_ClubId",
                table: "ParentPushMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_ParentPushSettings_Clubs_ClubId",
                table: "ParentPushSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_ParentPushSubscriptions_Clubs_ClubId",
                table: "ParentPushSubscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_ParentPushSubscriptions_Students_ClubId_StudentId",
                table: "ParentPushSubscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_ParentScheduleStates_Clubs_ClubId",
                table: "ParentScheduleStates");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Clubs_ClubId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_FamilyPayments_ClubId_FamilyPaymentId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_PersonalCharges_ClubId_PersonalChargeId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Sales_ClubId_SaleId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Students_ClubId_StudentId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_PendingBankPayments_Clubs_ClubId",
                table: "PendingBankPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_PendingBankPayments_Payments_ClubId_PaymentId",
                table: "PendingBankPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_PendingBankPayments_Students_ClubId_StudentId",
                table: "PendingBankPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonalCharges_Clubs_ClubId",
                table: "PersonalCharges");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonalCharges_PersonalTrainings_ClubId_PersonalTrainingId",
                table: "PersonalCharges");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonalTariffs_Clubs_ClubId",
                table: "PersonalTariffs");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonalTrainingAudits_Clubs_ClubId",
                table: "PersonalTrainingAudits");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonalTrainingAudits_PersonalTariffs_ClubId_PersonalTarif~",
                table: "PersonalTrainingAudits");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonalTrainingAudits_PersonalTrainings_ClubId_PersonalTra~",
                table: "PersonalTrainingAudits");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonalTrainings_Clubs_ClubId",
                table: "PersonalTrainings");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonalTrainings_PersonalTariffs_ClubId_PersonalTariffId",
                table: "PersonalTrainings");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonalTrainings_Students_ClubId_StudentId",
                table: "PersonalTrainings");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonalTrainingSlots_Clubs_ClubId",
                table: "PersonalTrainingSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonalTrainingSlots_PersonalTrainings_ClubId_PersonalTrai~",
                table: "PersonalTrainingSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerEvaluations_Clubs_ClubId",
                table: "PlayerEvaluations");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerEvaluations_Students_ClubId_StudentId",
                table: "PlayerEvaluations");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerFamilies_Clubs_ClubId",
                table: "PlayerFamilies");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Clubs_ClubId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariants_Clubs_ClubId",
                table: "ProductVariants");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariants_Products_ClubId_ProductId",
                table: "ProductVariants");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleDetails_Clubs_ClubId",
                table: "SaleDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleDetails_ProductVariants_ClubId_ProductId_ProductVariant~",
                table: "SaleDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleDetails_Products_ClubId_ProductId",
                table: "SaleDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleDetails_Sales_ClubId_SaleId",
                table: "SaleDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Clubs_ClubId",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Students_ClubId_StudentId",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK_SizeGroups_Clubs_ClubId",
                table: "SizeGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_SponsorshipAllocations_Clubs_ClubId",
                table: "SponsorshipAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_SponsorshipAllocations_SponsorshipContracts_ClubId_Contract~",
                table: "SponsorshipAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_SponsorshipAllocations_SponsorshipPlanLines_ClubId_PlanLine~",
                table: "SponsorshipAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_SponsorshipAudits_Clubs_ClubId",
                table: "SponsorshipAudits");

            migrationBuilder.DropForeignKey(
                name: "FK_SponsorshipBeneficiaries_Clubs_ClubId",
                table: "SponsorshipBeneficiaries");

            migrationBuilder.DropForeignKey(
                name: "FK_SponsorshipContracts_Clubs_ClubId",
                table: "SponsorshipContracts");

            migrationBuilder.DropForeignKey(
                name: "FK_SponsorshipContracts_SponsorshipPlans_ClubId_PlanId",
                table: "SponsorshipContracts");

            migrationBuilder.DropForeignKey(
                name: "FK_SponsorshipPlanLines_Clubs_ClubId",
                table: "SponsorshipPlanLines");

            migrationBuilder.DropForeignKey(
                name: "FK_SponsorshipPlanLines_SponsorshipPlans_ClubId_PlanId",
                table: "SponsorshipPlanLines");

            migrationBuilder.DropForeignKey(
                name: "FK_SponsorshipPlans_Clubs_ClubId",
                table: "SponsorshipPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_SponsorshipReceipts_Clubs_ClubId",
                table: "SponsorshipReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_SponsorshipReceipts_SponsorshipContracts_ClubId_ContractId",
                table: "SponsorshipReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_SponsorshipSpending_Clubs_ClubId",
                table: "SponsorshipSpending");

            migrationBuilder.DropForeignKey(
                name: "FK_SponsorshipSpending_SponsorshipAllocations_ClubId_Allocatio~",
                table: "SponsorshipSpending");

            migrationBuilder.DropForeignKey(
                name: "FK_SponsorshipSpending_SponsorshipBeneficiaries_ClubId_Benefic~",
                table: "SponsorshipSpending");

            migrationBuilder.DropForeignKey(
                name: "FK_StockEntries_Clubs_ClubId",
                table: "StockEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_StockEntryDetails_Clubs_ClubId",
                table: "StockEntryDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_StockEntryDetails_ProductVariants_ClubId_ProductId_ProductV~",
                table: "StockEntryDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_StockEntryDetails_Products_ClubId_ProductId",
                table: "StockEntryDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_StockEntryDetails_StockEntries_ClubId_StockEntryId",
                table: "StockEntryDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Clubs_ClubId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_ProductVariants_ClubId_ProductId_ProductVari~",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Products_ClubId_ProductId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentChanges_Clubs_ClubId",
                table: "StudentChanges");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentChanges_Students_ClubId_StudentId",
                table: "StudentChanges");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentFeeAssignments_Clubs_ClubId",
                table: "StudentFeeAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentFeeAssignments_FeePlans_ClubId_FeePlanId",
                table: "StudentFeeAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentFeeAssignments_PlayerFamilies_ClubId_FamilyId",
                table: "StudentFeeAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentFeeAssignments_Students_ClubId_StudentId",
                table: "StudentFeeAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentMonthlyExemptions_Clubs_ClubId",
                table: "StudentMonthlyExemptions");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentMonthlyExemptions_Students_ClubId_StudentId",
                table: "StudentMonthlyExemptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Clubs_ClubId",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_TrainingTeams_ClubId_TrainingTeamId",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentTeamChanges_Clubs_ClubId",
                table: "StudentTeamChanges");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentTeamChanges_Students_ClubId_StudentId",
                table: "StudentTeamChanges");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentTransactions_Clubs_ClubId",
                table: "StudentTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentTransactions_Students_ClubId_StudentId",
                table: "StudentTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamAttendances_Clubs_ClubId",
                table: "TeamAttendances");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamAttendances_TrainingTeams_ClubId_TrainingTeamId",
                table: "TeamAttendances");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamChanges_Clubs_ClubId",
                table: "TeamChanges");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamChanges_TrainingTeams_ClubId_TrainingTeamId",
                table: "TeamChanges");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingSessions_Clubs_ClubId",
                table: "TrainingSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingSessions_TrainingTeams_ClubId_TrainingTeamId",
                table: "TrainingSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingTeams_Clubs_ClubId",
                table: "TrainingTeams");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingTeams_FootballFields_ClubId_FootballFieldId",
                table: "TrainingTeams");

            migrationBuilder.DropForeignKey(
                name: "FK_TreasuryEntries_Clubs_ClubId",
                table: "TreasuryEntries");

            migrationBuilder.DropTable(
                name: "Clubs");

            migrationBuilder.DropIndex(
                name: "IX_TreasuryEntries_ClubId_Method",
                table: "TreasuryEntries");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_TrainingTeams_ClubId_Id",
                table: "TrainingTeams");

            migrationBuilder.DropIndex(
                name: "IX_TrainingTeams_ClubId_FootballFieldId",
                table: "TrainingTeams");

            migrationBuilder.DropIndex(
                name: "IX_TrainingSessions_ClubId_TrainingTeamId",
                table: "TrainingSessions");

            migrationBuilder.DropIndex(
                name: "IX_TeamChanges_ClubId_TrainingTeamId",
                table: "TeamChanges");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_TeamAttendances_ClubId_Id",
                table: "TeamAttendances");

            migrationBuilder.DropIndex(
                name: "IX_TeamAttendances_ClubId_TrainingTeamId_Date",
                table: "TeamAttendances");

            migrationBuilder.DropIndex(
                name: "IX_TeamAttendances_TrainingTeamId",
                table: "TeamAttendances");

            migrationBuilder.DropIndex(
                name: "IX_StudentTransactions_ClubId_StudentId",
                table: "StudentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_StudentTeamChanges_ClubId_StudentId",
                table: "StudentTeamChanges");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Students_ClubId_Id",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_ClubId_TrainingTeamId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_StudentMonthlyExemptions_ClubId_StudentId",
                table: "StudentMonthlyExemptions");

            migrationBuilder.DropIndex(
                name: "IX_StudentFeeAssignments_ClubId_FamilyId",
                table: "StudentFeeAssignments");

            migrationBuilder.DropIndex(
                name: "IX_StudentFeeAssignments_ClubId_FeePlanId",
                table: "StudentFeeAssignments");

            migrationBuilder.DropIndex(
                name: "IX_StudentFeeAssignments_ClubId_StudentId",
                table: "StudentFeeAssignments");

            migrationBuilder.DropIndex(
                name: "IX_StudentChanges_ClubId_StudentId",
                table: "StudentChanges");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ClubId_ProductId_ProductVariantId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockEntryDetails_ClubId_ProductId_ProductVariantId",
                table: "StockEntryDetails");

            migrationBuilder.DropIndex(
                name: "IX_StockEntryDetails_ClubId_StockEntryId",
                table: "StockEntryDetails");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_StockEntries_ClubId_Id",
                table: "StockEntries");

            migrationBuilder.DropIndex(
                name: "IX_StockEntries_ClubId_DocumentNumber",
                table: "StockEntries");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipSpending_BeneficiaryId",
                table: "SponsorshipSpending");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipSpending_ClubId_AllocationId",
                table: "SponsorshipSpending");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipSpending_ClubId_BankReference",
                table: "SponsorshipSpending");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipSpending_ClubId_BeneficiaryId_DocumentReference",
                table: "SponsorshipSpending");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipSpending_ClubId_BeneficiaryId_SalaryMonth",
                table: "SponsorshipSpending");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipReceipts_ClubId_BankReference",
                table: "SponsorshipReceipts");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipReceipts_ClubId_ContractId",
                table: "SponsorshipReceipts");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_SponsorshipPlans_ClubId_Id",
                table: "SponsorshipPlans");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipPlans_ClubId_Year",
                table: "SponsorshipPlans");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_SponsorshipPlanLines_ClubId_Id",
                table: "SponsorshipPlanLines");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipPlanLines_ClubId_PlanId_Category",
                table: "SponsorshipPlanLines");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipPlanLines_PlanId",
                table: "SponsorshipPlanLines");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_SponsorshipContracts_ClubId_Id",
                table: "SponsorshipContracts");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipContracts_ClubId_PlanId",
                table: "SponsorshipContracts");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipContracts_ClubId_SponsorNumber_Reference",
                table: "SponsorshipContracts");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_SponsorshipBeneficiaries_ClubId_Id",
                table: "SponsorshipBeneficiaries");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipBeneficiaries_ClubId_Identifier",
                table: "SponsorshipBeneficiaries");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipAudits_ClubId",
                table: "SponsorshipAudits");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_SponsorshipAllocations_ClubId_Id",
                table: "SponsorshipAllocations");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipAllocations_ClubId_ContractId_PlanLineId",
                table: "SponsorshipAllocations");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipAllocations_ClubId_PlanLineId",
                table: "SponsorshipAllocations");

            migrationBuilder.DropIndex(
                name: "IX_SponsorshipAllocations_ContractId",
                table: "SponsorshipAllocations");

            migrationBuilder.DropIndex(
                name: "IX_SizeGroups_ClubId",
                table: "SizeGroups");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Sales_ClubId_Id",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_ClubId_StudentId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_SaleDetails_ClubId_ProductId_ProductVariantId",
                table: "SaleDetails");

            migrationBuilder.DropIndex(
                name: "IX_SaleDetails_ClubId_SaleId",
                table: "SaleDetails");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ProductVariants_ClubId_ProductId_Id",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ClubId_ProductId_Size",
                table: "ProductVariants");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Products_ClubId_Id",
                table: "Products");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_PlayerFamilies_ClubId_Id",
                table: "PlayerFamilies");

            migrationBuilder.DropIndex(
                name: "IX_PlayerEvaluations_ClubId_StudentId",
                table: "PlayerEvaluations");

            migrationBuilder.DropIndex(
                name: "IX_PersonalTrainingSlots_ClubId_PersonalTrainingId",
                table: "PersonalTrainingSlots");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_PersonalTrainings_ClubId_Id",
                table: "PersonalTrainings");

            migrationBuilder.DropIndex(
                name: "IX_PersonalTrainings_ClubId_PersonalTariffId",
                table: "PersonalTrainings");

            migrationBuilder.DropIndex(
                name: "IX_PersonalTrainings_ClubId_StudentId",
                table: "PersonalTrainings");

            migrationBuilder.DropIndex(
                name: "IX_PersonalTrainingAudits_ClubId_PersonalTariffId",
                table: "PersonalTrainingAudits");

            migrationBuilder.DropIndex(
                name: "IX_PersonalTrainingAudits_ClubId_PersonalTrainingId",
                table: "PersonalTrainingAudits");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_PersonalTariffs_ClubId_Id",
                table: "PersonalTariffs");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_PersonalCharges_ClubId_Id",
                table: "PersonalCharges");

            migrationBuilder.DropIndex(
                name: "IX_PersonalCharges_ClubId_PersonalTrainingId_Period",
                table: "PersonalCharges");

            migrationBuilder.DropIndex(
                name: "IX_PersonalCharges_PersonalTrainingId",
                table: "PersonalCharges");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_PendingBankPayments_ClubId_Id",
                table: "PendingBankPayments");

            migrationBuilder.DropIndex(
                name: "IX_PendingBankPayments_ClubId_PaymentId",
                table: "PendingBankPayments");

            migrationBuilder.DropIndex(
                name: "IX_PendingBankPayments_ClubId_StudentId",
                table: "PendingBankPayments");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Payments_ClubId_Id",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ClubId_FamilyPaymentId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ClubId_PersonalChargeId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ClubId_SaleId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ClubId_StudentId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_ParentScheduleStates_ClubId",
                table: "ParentScheduleStates");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ParentPushSubscriptions_ClubId_Id",
                table: "ParentPushSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_ParentPushSubscriptions_ClubId_StudentId_EndpointHash",
                table: "ParentPushSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_ParentPushSubscriptions_StudentId",
                table: "ParentPushSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_ParentPushSettings_ClubId",
                table: "ParentPushSettings");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ParentPushMessages_ClubId_Id",
                table: "ParentPushMessages");

            migrationBuilder.DropIndex(
                name: "IX_ParentPushDeliveries_ClubId_MessageId_SubscriptionId",
                table: "ParentPushDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_ParentPushDeliveries_ClubId_SubscriptionId",
                table: "ParentPushDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_ParentPushDeliveries_MessageId",
                table: "ParentPushDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_NutritionMenus_ClubId_Scope",
                table: "NutritionMenus");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_MonthlyFeePeriods_ClubId_Id",
                table: "MonthlyFeePeriods");

            migrationBuilder.DropIndex(
                name: "IX_MonthlyFeePeriods_ClubId_StudentId_Year_Month",
                table: "MonthlyFeePeriods");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_FootballFields_ClubId_Id",
                table: "FootballFields");

            migrationBuilder.DropIndex(
                name: "IX_FirstMonthFeeChanges_ClubId_PeriodId",
                table: "FirstMonthFeeChanges");

            migrationBuilder.DropIndex(
                name: "IX_FieldPayments_ClubId_FieldBookingId",
                table: "FieldPayments");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_FieldBookings_ClubId_Id",
                table: "FieldBookings");

            migrationBuilder.DropIndex(
                name: "IX_FieldBookings_ClubId_FootballFieldId",
                table: "FieldBookings");

            migrationBuilder.DropIndex(
                name: "IX_FieldBookingChanges_ClubId_FieldBookingId",
                table: "FieldBookingChanges");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_FeePlans_ClubId_Id",
                table: "FeePlans");

            migrationBuilder.DropIndex(
                name: "IX_FeePlanPrices_ClubId_FeePlanId",
                table: "FeePlanPrices");

            migrationBuilder.DropIndex(
                name: "IX_FamilyTariffAssignments_ClubId_FamilyId",
                table: "FamilyTariffAssignments");

            migrationBuilder.DropIndex(
                name: "IX_FamilyTariffAssignments_ClubId_FeePlanId",
                table: "FamilyTariffAssignments");

            migrationBuilder.DropIndex(
                name: "IX_FamilyRegistrations_ClubId_FamilyId",
                table: "FamilyRegistrations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_FamilyPayments_ClubId_Id",
                table: "FamilyPayments");

            migrationBuilder.DropIndex(
                name: "IX_FamilyPayments_ClubId_FamilyId",
                table: "FamilyPayments");

            migrationBuilder.DropIndex(
                name: "IX_FamilyChanges_ClubId_FamilyId",
                table: "FamilyChanges");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_ClubId_CategoryId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_ClubId_SubcategoryId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseCategoryChanges_ClubId_ExpenseCategoryId",
                table: "ExpenseCategoryChanges");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ExpenseCategories_ClubId_Id",
                table: "ExpenseCategories");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseCategories_ClubId_ParentId_Name",
                table: "ExpenseCategories");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseCategories_ParentId",
                table: "ExpenseCategories");

            migrationBuilder.DropIndex(
                name: "IX_DevelopmentRecords_ClubId_StudentId",
                table: "DevelopmentRecords");

            migrationBuilder.DropIndex(
                name: "IX_DemoImports_ClubId",
                table: "DemoImports");

            migrationBuilder.DropIndex(
                name: "IX_CoachTeams_ClubId_TrainingTeamId",
                table: "CoachTeams");

            migrationBuilder.DropIndex(
                name: "IX_ClubSettings_ClubId",
                table: "ClubSettings");

            migrationBuilder.DropIndex(
                name: "IX_CareReferrals_ClubId_StudentId",
                table: "CareReferrals");

            migrationBuilder.DropIndex(
                name: "IX_CareEntries_ClubId_AssignmentId",
                table: "CareEntries");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_CareAssignments_ClubId_Id",
                table: "CareAssignments");

            migrationBuilder.DropIndex(
                name: "IX_CareAssignments_ClubId_StaffId_StudentId_Kind",
                table: "CareAssignments");

            migrationBuilder.DropIndex(
                name: "IX_CareAssignments_ClubId_StudentId",
                table: "CareAssignments");

            migrationBuilder.DropIndex(
                name: "IX_BankPaymentAllocations_ClubId_BankPaymentId",
                table: "BankPaymentAllocations");

            migrationBuilder.DropIndex(
                name: "IX_BankPaymentAllocations_ClubId_PaymentId",
                table: "BankPaymentAllocations");

            migrationBuilder.DropIndex(
                name: "IX_BankPaymentAllocations_PaymentId",
                table: "BankPaymentAllocations");

            migrationBuilder.DropIndex(
                name: "IX_AttendancePlayers_ClubId_StudentId",
                table: "AttendancePlayers");

            migrationBuilder.DropIndex(
                name: "IX_AttendancePlayers_ClubId_TeamAttendanceId_StudentId",
                table: "AttendancePlayers");

            migrationBuilder.DropIndex(
                name: "IX_AttendancePlayers_TeamAttendanceId",
                table: "AttendancePlayers");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceChanges_ClubId_TeamAttendanceId",
                table: "AttendanceChanges");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ClubId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "TreasuryEntries");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "TrainingTeams");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "TrainingSessions");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "TeamChanges");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "TeamAttendances");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "StudentTransactions");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "StudentTeamChanges");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "StudentMonthlyExemptions");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "StudentFeeAssignments");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "StudentChanges");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "StockEntryDetails");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "StockEntries");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "SponsorshipSpending");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "SponsorshipReceipts");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "SponsorshipPlans");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "SponsorshipPlanLines");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "SponsorshipContracts");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "SponsorshipBeneficiaries");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "SponsorshipAudits");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "SponsorshipAllocations");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "SizeGroups");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "SaleDetails");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "PlayerFamilies");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "PlayerEvaluations");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "PersonalTrainingSlots");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "PersonalTrainings");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "PersonalTrainingAudits");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "PersonalTariffs");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "PersonalCharges");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "PendingBankPayments");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "ParentScheduleStates");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "ParentPushSubscriptions");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "ParentPushSettings");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "ParentPushMessages");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "ParentPushDeliveries");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "NutritionMenus");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "MonthlyFeePeriods");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "FootballFields");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "FirstMonthFeeChanges");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "FieldPayments");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "FieldBookings");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "FieldBookingChanges");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "FeePlans");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "FeePlanPrices");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "FamilyTariffAssignments");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "FamilyRegistrations");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "FamilyPayments");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "FamilyChanges");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "ExpenseCategoryChanges");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "ExpenseCategories");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "DevelopmentRecords");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "DemoImports");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "CoachTeams");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "ClubSettings");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "CareReferrals");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "CareEntries");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "CareAssignments");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "BankPaymentAllocations");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "AttendancePlayers");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "AttendanceChanges");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsPlatformOwner",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_TreasuryEntries_Method",
                table: "TreasuryEntries",
                column: "Method",
                unique: true,
                filter: "\"Kind\" = 'Opening' AND \"CancelledAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TeamAttendances_TrainingTeamId_Date",
                table: "TeamAttendances",
                columns: new[] { "TrainingTeamId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockEntries_DocumentNumber",
                table: "StockEntries",
                column: "DocumentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipSpending_BankReference",
                table: "SponsorshipSpending",
                column: "BankReference",
                unique: true,
                filter: "\"CancelledAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipSpending_BeneficiaryId_DocumentReference",
                table: "SponsorshipSpending",
                columns: new[] { "BeneficiaryId", "DocumentReference" },
                unique: true,
                filter: "\"CancelledAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipSpending_BeneficiaryId_SalaryMonth",
                table: "SponsorshipSpending",
                columns: new[] { "BeneficiaryId", "SalaryMonth" },
                unique: true,
                filter: "\"CancelledAt\" IS NULL AND \"SalaryMonth\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipReceipts_BankReference",
                table: "SponsorshipReceipts",
                column: "BankReference",
                unique: true,
                filter: "\"CancelledAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipPlans_Year",
                table: "SponsorshipPlans",
                column: "Year",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipPlanLines_PlanId_Category",
                table: "SponsorshipPlanLines",
                columns: new[] { "PlanId", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipContracts_SponsorNumber_Reference",
                table: "SponsorshipContracts",
                columns: new[] { "SponsorNumber", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipBeneficiaries_Identifier",
                table: "SponsorshipBeneficiaries",
                column: "Identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipAllocations_ContractId_PlanLineId",
                table: "SponsorshipAllocations",
                columns: new[] { "ContractId", "PlanLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_Size",
                table: "ProductVariants",
                columns: new[] { "ProductId", "Size" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonalCharges_PersonalTrainingId_Period",
                table: "PersonalCharges",
                columns: new[] { "PersonalTrainingId", "Period" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParentPushSubscriptions_StudentId_EndpointHash",
                table: "ParentPushSubscriptions",
                columns: new[] { "StudentId", "EndpointHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParentPushDeliveries_MessageId_SubscriptionId",
                table: "ParentPushDeliveries",
                columns: new[] { "MessageId", "SubscriptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyFeePeriods_StudentId_Year_Month",
                table: "MonthlyFeePeriods",
                columns: new[] { "StudentId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_ParentId_Name",
                table: "ExpenseCategories",
                columns: new[] { "ParentId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankPaymentAllocations_PaymentId",
                table: "BankPaymentAllocations",
                column: "PaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendancePlayers_TeamAttendanceId_StudentId",
                table: "AttendancePlayers",
                columns: new[] { "TeamAttendanceId", "StudentId" },
                unique: true);
        }
    }
}
