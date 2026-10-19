using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class ParentPushNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParentPushMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Body = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Audience = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentPushMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParentPushSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    PublicKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrivateKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentPushSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParentPushSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Endpoint = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    EndpointHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    P256dh = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Auth = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentPushSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParentPushSubscriptions_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParentPushDeliveries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageId = table.Column<int>(type: "integer", nullable: false),
                    SubscriptionId = table.Column<int>(type: "integer", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    NextAttempt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Failed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentPushDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParentPushDeliveries_ParentPushMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "ParentPushMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParentPushDeliveries_ParentPushSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "ParentPushSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParentPushDeliveries_MessageId_SubscriptionId",
                table: "ParentPushDeliveries",
                columns: new[] { "MessageId", "SubscriptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParentPushDeliveries_SentAt_Failed_NextAttempt",
                table: "ParentPushDeliveries",
                columns: new[] { "SentAt", "Failed", "NextAttempt" });

            migrationBuilder.CreateIndex(
                name: "IX_ParentPushDeliveries_SubscriptionId",
                table: "ParentPushDeliveries",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentPushMessages_RequestId",
                table: "ParentPushMessages",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParentPushSubscriptions_StudentId_EndpointHash",
                table: "ParentPushSubscriptions",
                columns: new[] { "StudentId", "EndpointHash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParentPushDeliveries");

            migrationBuilder.DropTable(
                name: "ParentPushSettings");

            migrationBuilder.DropTable(
                name: "ParentPushMessages");

            migrationBuilder.DropTable(
                name: "ParentPushSubscriptions");
        }
    }
}
