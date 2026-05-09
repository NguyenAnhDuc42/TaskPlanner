using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveInheritWorkflowFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_inheriting_workflow",
                table: "project_spaces");

            migrationBuilder.DropColumn(
                name: "is_inheriting_workflow",
                table: "project_folders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_inheriting_workflow",
                table: "project_spaces",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_inheriting_workflow",
                table: "project_folders",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }
    }
}
