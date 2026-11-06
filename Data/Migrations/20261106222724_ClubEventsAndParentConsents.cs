using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class ClubEventsAndParentConsents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EventInvitationId",
                table: "Payments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_SponsorshipReceipts_ClubId_Id",
                table: "SponsorshipReceipts",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Expenses_ClubId_Id",
                table: "Expenses",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.CreateTable(
                name: "EventCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ClubId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventCategories", x => x.Id);
                    table.UniqueConstraint("AK_EventCategories_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_EventCategories_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClubEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Location = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RequiresConsent = table.Column<bool>(type: "boolean", nullable: false),
                    Terms = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Transport = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
                    Instructions = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
                    Organizer = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Contact = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CancellationPolicy = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Photo = table.Column<byte[]>(type: "bytea", nullable: true),
                    PhotoType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClubId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubEvents", x => x.Id);
                    table.UniqueConstraint("AK_ClubEvents_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_ClubEvents_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClubEvents_EventCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "EventCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClubEvents_EventCategories_ClubId_CategoryId",
                        columns: x => new { x.ClubId, x.CategoryId },
                        principalTable: "EventCategories",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClubEventId = table.Column<int>(type: "integer", nullable: false),
                    Actor = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Action = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Snapshot = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClubId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventAudits_ClubEvents_ClubEventId",
                        column: x => x.ClubEventId,
                        principalTable: "ClubEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventAudits_ClubEvents_ClubId_ClubEventId",
                        columns: x => new { x.ClubId, x.ClubEventId },
                        principalTable: "ClubEvents",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventAudits_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventExpenseLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClubEventId = table.Column<int>(type: "integer", nullable: false),
                    ExpenseId = table.Column<int>(type: "integer", nullable: false),
                    ClubId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventExpenseLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventExpenseLinks_ClubEvents_ClubEventId",
                        column: x => x.ClubEventId,
                        principalTable: "ClubEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventExpenseLinks_ClubEvents_ClubId_ClubEventId",
                        columns: x => new { x.ClubId, x.ClubEventId },
                        principalTable: "ClubEvents",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventExpenseLinks_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventExpenseLinks_Expenses_ClubId_ExpenseId",
                        columns: x => new { x.ClubId, x.ExpenseId },
                        principalTable: "Expenses",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventExpenseLinks_Expenses_ExpenseId",
                        column: x => x.ExpenseId,
                        principalTable: "Expenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventInvitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClubEventId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AcceptedVersion = table.Column<int>(type: "integer", nullable: false),
                    Attended = table.Column<bool>(type: "boolean", nullable: true),
                    WithdrawalRequested = table.Column<bool>(type: "boolean", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Waived = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Paid = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ClubId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventInvitations", x => x.Id);
                    table.UniqueConstraint("AK_EventInvitations_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_EventInvitations_ClubEvents_ClubEventId",
                        column: x => x.ClubEventId,
                        principalTable: "ClubEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventInvitations_ClubEvents_ClubId_ClubEventId",
                        columns: x => new { x.ClubId, x.ClubEventId },
                        principalTable: "ClubEvents",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventInvitations_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventInvitations_Students_ClubId_StudentId",
                        columns: x => new { x.ClubId, x.StudentId },
                        principalTable: "Students",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventInvitations_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventSponsorLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClubEventId = table.Column<int>(type: "integer", nullable: false),
                    SponsorshipReceiptId = table.Column<int>(type: "integer", nullable: false),
                    ClubId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventSponsorLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventSponsorLinks_ClubEvents_ClubEventId",
                        column: x => x.ClubEventId,
                        principalTable: "ClubEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventSponsorLinks_ClubEvents_ClubId_ClubEventId",
                        columns: x => new { x.ClubId, x.ClubEventId },
                        principalTable: "ClubEvents",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventSponsorLinks_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventSponsorLinks_SponsorshipReceipts_ClubId_SponsorshipRec~",
                        columns: x => new { x.ClubId, x.SponsorshipReceiptId },
                        principalTable: "SponsorshipReceipts",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventSponsorLinks_SponsorshipReceipts_SponsorshipReceiptId",
                        column: x => x.SponsorshipReceiptId,
                        principalTable: "SponsorshipReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventConsents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventInvitationId = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Decision = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Guardian = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TermsAccepted = table.Column<bool>(type: "boolean", nullable: false),
                    Snapshot = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClubId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventConsents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventConsents_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventConsents_EventInvitations_ClubId_EventInvitationId",
                        columns: x => new { x.ClubId, x.EventInvitationId },
                        principalTable: "EventInvitations",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventConsents_EventInvitations_EventInvitationId",
                        column: x => x.EventInvitationId,
                        principalTable: "EventInvitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventSettlements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventInvitationId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Kind = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ExpenseId = table.Column<int>(type: "integer", nullable: true),
                    Actor = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClubId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventSettlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventSettlements_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventSettlements_EventInvitations_ClubId_EventInvitationId",
                        columns: x => new { x.ClubId, x.EventInvitationId },
                        principalTable: "EventInvitations",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventSettlements_EventInvitations_EventInvitationId",
                        column: x => x.EventInvitationId,
                        principalTable: "EventInvitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventSettlements_Expenses_ClubId_ExpenseId",
                        columns: x => new { x.ClubId, x.ExpenseId },
                        principalTable: "Expenses",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventSettlements_Expenses_ExpenseId",
                        column: x => x.ExpenseId,
                        principalTable: "Expenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ClubId_EventInvitationId",
                table: "Payments",
                columns: new[] { "ClubId", "EventInvitationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_EventInvitationId",
                table: "Payments",
                column: "EventInvitationId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubEvents_CategoryId",
                table: "ClubEvents",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubEvents_ClubId_CategoryId",
                table: "ClubEvents",
                columns: new[] { "ClubId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventAudits_ClubEventId",
                table: "EventAudits",
                column: "ClubEventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventAudits_ClubId_ClubEventId",
                table: "EventAudits",
                columns: new[] { "ClubId", "ClubEventId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventCategories_ClubId_Name",
                table: "EventCategories",
                columns: new[] { "ClubId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventConsents_ClubId_EventInvitationId",
                table: "EventConsents",
                columns: new[] { "ClubId", "EventInvitationId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventConsents_EventInvitationId",
                table: "EventConsents",
                column: "EventInvitationId");

            migrationBuilder.CreateIndex(
                name: "IX_EventExpenseLinks_ClubEventId",
                table: "EventExpenseLinks",
                column: "ClubEventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventExpenseLinks_ClubId_ClubEventId",
                table: "EventExpenseLinks",
                columns: new[] { "ClubId", "ClubEventId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventExpenseLinks_ClubId_ExpenseId",
                table: "EventExpenseLinks",
                columns: new[] { "ClubId", "ExpenseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventExpenseLinks_ExpenseId",
                table: "EventExpenseLinks",
                column: "ExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_EventInvitations_ClubEventId",
                table: "EventInvitations",
                column: "ClubEventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventInvitations_ClubId_ClubEventId_StudentId",
                table: "EventInvitations",
                columns: new[] { "ClubId", "ClubEventId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventInvitations_ClubId_StudentId",
                table: "EventInvitations",
                columns: new[] { "ClubId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventInvitations_StudentId",
                table: "EventInvitations",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_EventSettlements_ClubId_EventInvitationId",
                table: "EventSettlements",
                columns: new[] { "ClubId", "EventInvitationId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventSettlements_ClubId_ExpenseId",
                table: "EventSettlements",
                columns: new[] { "ClubId", "ExpenseId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventSettlements_EventInvitationId",
                table: "EventSettlements",
                column: "EventInvitationId");

            migrationBuilder.CreateIndex(
                name: "IX_EventSettlements_ExpenseId",
                table: "EventSettlements",
                column: "ExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_EventSettlements_RequestId",
                table: "EventSettlements",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventSponsorLinks_ClubEventId",
                table: "EventSponsorLinks",
                column: "ClubEventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventSponsorLinks_ClubId_ClubEventId",
                table: "EventSponsorLinks",
                columns: new[] { "ClubId", "ClubEventId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventSponsorLinks_ClubId_SponsorshipReceiptId",
                table: "EventSponsorLinks",
                columns: new[] { "ClubId", "SponsorshipReceiptId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventSponsorLinks_SponsorshipReceiptId",
                table: "EventSponsorLinks",
                column: "SponsorshipReceiptId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_EventInvitations_ClubId_EventInvitationId",
                table: "Payments",
                columns: new[] { "ClubId", "EventInvitationId" },
                principalTable: "EventInvitations",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_EventInvitations_EventInvitationId",
                table: "Payments",
                column: "EventInvitationId",
                principalTable: "EventInvitations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_EventInvitations_ClubId_EventInvitationId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_EventInvitations_EventInvitationId",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "EventAudits");

            migrationBuilder.DropTable(
                name: "EventConsents");

            migrationBuilder.DropTable(
                name: "EventExpenseLinks");

            migrationBuilder.DropTable(
                name: "EventSettlements");

            migrationBuilder.DropTable(
                name: "EventSponsorLinks");

            migrationBuilder.DropTable(
                name: "EventInvitations");

            migrationBuilder.DropTable(
                name: "ClubEvents");

            migrationBuilder.DropTable(
                name: "EventCategories");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_SponsorshipReceipts_ClubId_Id",
                table: "SponsorshipReceipts");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ClubId_EventInvitationId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_EventInvitationId",
                table: "Payments");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Expenses_ClubId_Id",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "EventInvitationId",
                table: "Payments");
        }
    }
}
