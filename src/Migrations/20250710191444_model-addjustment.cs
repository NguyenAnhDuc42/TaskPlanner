using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace src.Migrations
{
    /// <inheritdoc />
    public partial class modeladdjustment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatorName",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "CreatorName",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "CreatorName",
                table: "Spaces");

            migrationBuilder.DropColumn(
                name: "CreatorName",
                table: "Lists");

            migrationBuilder.DropColumn(
                name: "CreatorName",
                table: "Folders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatorName",
                table: "Workspaces",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatorName",
                table: "Tasks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatorName",
                table: "Spaces",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatorName",
                table: "Lists",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatorName",
                table: "Folders",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
