using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2Korriku.Data.Migrations
{
    /// <inheritdoc />
    public partial class ParentEquipmentOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"ClubSettings\" SET \"ParentPortalModules\" = CASE WHEN \"ParentPortalModules\" = '' THEN 'Equipment' ELSE \"ParentPortalModules\" || ',Equipment' END WHERE NOT ('Equipment' = ANY(string_to_array(\"ParentPortalModules\", ',')))");
            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "Sales",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveredById",
                table: "Sales",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsParentOrder",
                table: "Sales",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "Photo",
                table: "Products",
                type: "bytea",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ParentPortalModules",
                table: "ClubSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "Attendance,Finance,Events,Progress,Menu,Doctor,Psychologist,Chat,Parking,Notifications,Photo,Equipment",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldDefaultValue: "Attendance,Finance,Events,Progress,Menu,Doctor,Psychologist,Chat,Parking,Notifications,Photo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"ClubSettings\" SET \"ParentPortalModules\" = array_to_string(array_remove(string_to_array(\"ParentPortalModules\", ','), 'Equipment'), ',')");
            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "DeliveredById",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "IsParentOrder",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "Photo",
                table: "Products");

            migrationBuilder.AlterColumn<string>(
                name: "ParentPortalModules",
                table: "ClubSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "Attendance,Finance,Events,Progress,Menu,Doctor,Psychologist,Chat,Parking,Notifications,Photo",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldDefaultValue: "Attendance,Finance,Events,Progress,Menu,Doctor,Psychologist,Chat,Parking,Notifications,Photo,Equipment");
        }
    }
}
