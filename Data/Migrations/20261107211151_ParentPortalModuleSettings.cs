using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class ParentPortalModuleSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ParentPortalModules",
                table: "ClubSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "Attendance,Finance,Events,Progress,Menu,Doctor,Psychologist,Chat,Parking,Notifications,Photo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParentPortalModules",
                table: "ClubSettings");
        }
    }
}
