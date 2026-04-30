using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameSpaceAndFolderIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "space_id",
                table: "workflows",
                newName: "project_space_id");

            migrationBuilder.RenameColumn(
                name: "folder_id",
                table: "workflows",
                newName: "project_folder_id");

            migrationBuilder.RenameColumn(
                name: "space_id",
                table: "view_definitions",
                newName: "project_space_id");

            migrationBuilder.RenameColumn(
                name: "folder_id",
                table: "view_definitions",
                newName: "project_folder_id");

            migrationBuilder.RenameColumn(
                name: "sort_order",
                table: "view_definitions",
                newName: "order_key");

            migrationBuilder.RenameIndex(
                name: "IX_view_definitions_space_id",
                table: "view_definitions",
                newName: "IX_view_definitions_project_space_id");

            migrationBuilder.RenameIndex(
                name: "IX_view_definitions_folder_id",
                table: "view_definitions",
                newName: "IX_view_definitions_project_folder_id");

            migrationBuilder.AddColumn<string>(
                name: "order_key",
                table: "statuses",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_statuses_workflow_id_order_key",
                table: "statuses",
                columns: new[] { "workflow_id", "order_key" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_statuses_workflow_id_order_key",
                table: "statuses");

            migrationBuilder.DropColumn(
                name: "order_key",
                table: "statuses");

            migrationBuilder.RenameColumn(
                name: "project_space_id",
                table: "workflows",
                newName: "space_id");

            migrationBuilder.RenameColumn(
                name: "project_folder_id",
                table: "workflows",
                newName: "folder_id");

            migrationBuilder.RenameColumn(
                name: "project_space_id",
                table: "view_definitions",
                newName: "space_id");

            migrationBuilder.RenameColumn(
                name: "project_folder_id",
                table: "view_definitions",
                newName: "folder_id");

            migrationBuilder.RenameColumn(
                name: "order_key",
                table: "view_definitions",
                newName: "sort_order");

            migrationBuilder.RenameIndex(
                name: "IX_view_definitions_project_space_id",
                table: "view_definitions",
                newName: "IX_view_definitions_space_id");

            migrationBuilder.RenameIndex(
                name: "IX_view_definitions_project_folder_id",
                table: "view_definitions",
                newName: "IX_view_definitions_folder_id");
        }
    }
}
