using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class AllBankVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "StudentId",
                table: "PendingBankPayments",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "PendingBankPayments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResultId",
                table: "PendingBankPayments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetId",
                table: "PendingBankPayments",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "PendingBankPayments");

            migrationBuilder.DropColumn(
                name: "ResultId",
                table: "PendingBankPayments");

            migrationBuilder.DropColumn(
                name: "TargetId",
                table: "PendingBankPayments");

            migrationBuilder.AlterColumn<int>(
                name: "StudentId",
                table: "PendingBankPayments",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
