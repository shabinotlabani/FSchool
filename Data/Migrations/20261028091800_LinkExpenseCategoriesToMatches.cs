using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkExpenseCategoriesToMatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContextType",
                table: "ExpenseCategories",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContextType",
                table: "ExpenseCategories");
        }
    }
}
