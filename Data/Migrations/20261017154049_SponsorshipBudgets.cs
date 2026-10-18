using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class SponsorshipBudgets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SponsorshipAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Entity = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    EntityId = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Before = table.Column<string>(type: "text", nullable: false),
                    After = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SponsorshipAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SponsorshipBeneficiaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Identifier = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    IsPerson = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SponsorshipBeneficiaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SponsorshipPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SponsorshipPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SponsorshipContracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<int>(type: "integer", nullable: false),
                    Sponsor = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SponsorNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    From = table.Column<DateOnly>(type: "date", nullable: false),
                    To = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SponsorshipContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SponsorshipContracts_SponsorshipPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SponsorshipPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SponsorshipPlanLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlanId = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Activities = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsSalary = table.Column<bool>(type: "boolean", nullable: false),
                    Budget = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SponsorshipPlanLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SponsorshipPlanLines_SponsorshipPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SponsorshipPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SponsorshipReceipts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ContractId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BankReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SponsorshipReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SponsorshipReceipts_SponsorshipContracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "SponsorshipContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SponsorshipAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContractId = table.Column<int>(type: "integer", nullable: false),
                    PlanLineId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SponsorshipAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SponsorshipAllocations_SponsorshipContracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "SponsorshipContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SponsorshipAllocations_SponsorshipPlanLines_PlanLineId",
                        column: x => x.PlanLineId,
                        principalTable: "SponsorshipPlanLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SponsorshipSpending",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AllocationId = table.Column<int>(type: "integer", nullable: false),
                    BeneficiaryId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    SalaryMonth = table.Column<DateOnly>(type: "date", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DocumentReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BankReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SponsorshipSpending", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SponsorshipSpending_SponsorshipAllocations_AllocationId",
                        column: x => x.AllocationId,
                        principalTable: "SponsorshipAllocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SponsorshipSpending_SponsorshipBeneficiaries_BeneficiaryId",
                        column: x => x.BeneficiaryId,
                        principalTable: "SponsorshipBeneficiaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipAllocations_ContractId_PlanLineId",
                table: "SponsorshipAllocations",
                columns: new[] { "ContractId", "PlanLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipAllocations_PlanLineId",
                table: "SponsorshipAllocations",
                column: "PlanLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipAudits_Entity_EntityId",
                table: "SponsorshipAudits",
                columns: new[] { "Entity", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipBeneficiaries_Identifier",
                table: "SponsorshipBeneficiaries",
                column: "Identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipContracts_PlanId",
                table: "SponsorshipContracts",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipContracts_SponsorNumber_Reference",
                table: "SponsorshipContracts",
                columns: new[] { "SponsorNumber", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipPlanLines_PlanId_Category",
                table: "SponsorshipPlanLines",
                columns: new[] { "PlanId", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipPlans_Year",
                table: "SponsorshipPlans",
                column: "Year",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipReceipts_BankReference",
                table: "SponsorshipReceipts",
                column: "BankReference",
                unique: true,
                filter: "\"CancelledAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipReceipts_ContractId",
                table: "SponsorshipReceipts",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipReceipts_RequestId",
                table: "SponsorshipReceipts",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipSpending_AllocationId",
                table: "SponsorshipSpending",
                column: "AllocationId");

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
                name: "IX_SponsorshipSpending_RequestId",
                table: "SponsorshipSpending",
                column: "RequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SponsorshipAudits");

            migrationBuilder.DropTable(
                name: "SponsorshipReceipts");

            migrationBuilder.DropTable(
                name: "SponsorshipSpending");

            migrationBuilder.DropTable(
                name: "SponsorshipAllocations");

            migrationBuilder.DropTable(
                name: "SponsorshipBeneficiaries");

            migrationBuilder.DropTable(
                name: "SponsorshipContracts");

            migrationBuilder.DropTable(
                name: "SponsorshipPlanLines");

            migrationBuilder.DropTable(
                name: "SponsorshipPlans");
        }
    }
}
