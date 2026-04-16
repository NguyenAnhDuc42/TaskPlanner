using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UnUpdateHierarchyIndexFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_project_tasks_project_folder_id_order_key_id",
                table: "project_tasks");

            migrationBuilder.DropIndex(
                name: "IX_project_tasks_project_space_id_order_key_id",
                table: "project_tasks");

            migrationBuilder.DropIndex(
                name: "IX_project_spaces_project_workspace_id_order_key_id",
                table: "project_spaces");

            migrationBuilder.DropIndex(
                name: "IX_project_folders_project_workspace_id_project_space_id_order~",
                table: "project_folders");

            migrationBuilder.CreateIndex(
                name: "IX_project_tasks_project_folder_id_order_key_id",
                table: "project_tasks",
                columns: new[] { "project_folder_id", "order_key", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_project_tasks_project_space_id_order_key_id",
                table: "project_tasks",
                columns: new[] { "project_space_id", "order_key", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_project_spaces_project_workspace_id_order_key_id",
                table: "project_spaces",
                columns: new[] { "project_workspace_id", "order_key", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_project_folders_project_workspace_id_project_space_id_order~",
                table: "project_folders",
                columns: new[] { "project_workspace_id", "project_space_id", "order_key", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_project_tasks_project_folder_id_order_key_id",
                table: "project_tasks");

            migrationBuilder.DropIndex(
                name: "IX_project_tasks_project_space_id_order_key_id",
                table: "project_tasks");

            migrationBuilder.DropIndex(
                name: "IX_project_spaces_project_workspace_id_order_key_id",
                table: "project_spaces");

            migrationBuilder.DropIndex(
                name: "IX_project_folders_project_workspace_id_project_space_id_order~",
                table: "project_folders");

            migrationBuilder.CreateIndex(
                name: "IX_project_tasks_project_folder_id_order_key_id",
                table: "project_tasks",
                columns: new[] { "project_folder_id", "order_key", "id" },
                filter: "\"deleted_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_project_tasks_project_space_id_order_key_id",
                table: "project_tasks",
                columns: new[] { "project_space_id", "order_key", "id" },
                filter: "\"deleted_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_project_spaces_project_workspace_id_order_key_id",
                table: "project_spaces",
                columns: new[] { "project_workspace_id", "order_key", "id" },
                filter: "\"deleted_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_project_folders_project_workspace_id_project_space_id_order~",
                table: "project_folders",
                columns: new[] { "project_workspace_id", "project_space_id", "order_key", "id" },
                filter: "\"deleted_at\" IS NULL");
        }
    }
}
