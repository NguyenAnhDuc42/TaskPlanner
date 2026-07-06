using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaginationIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_project_tasks_folder_order_key",
                table: "project_tasks",
                columns: new[] { "project_folder_id", "order_key", "id" },
                filter: "deleted_at IS NULL AND is_archived = false");

            migrationBuilder.CreateIndex(
                name: "IX_project_tasks_space_order_key",
                table: "project_tasks",
                columns: new[] { "project_workspace_id", "project_space_id", "order_key", "id" },
                filter: "deleted_at IS NULL AND is_archived = false AND project_folder_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_project_spaces_workspace_order_key",
                table: "project_spaces",
                columns: new[] { "project_workspace_id", "order_key", "id" },
                filter: "deleted_at IS NULL AND is_archived = false");

            migrationBuilder.CreateIndex(
                name: "IX_favorites_member_workspace_order_key",
                table: "favorites",
                columns: new[] { "workspace_member_id", "project_workspace_id", "order_key", "id" },
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_project_tasks_folder_order_key",
                table: "project_tasks");

            migrationBuilder.DropIndex(
                name: "IX_project_tasks_space_order_key",
                table: "project_tasks");

            migrationBuilder.DropIndex(
                name: "IX_project_spaces_workspace_order_key",
                table: "project_spaces");

            migrationBuilder.DropIndex(
                name: "IX_favorites_member_workspace_order_key",
                table: "favorites");
        }
    }
}
