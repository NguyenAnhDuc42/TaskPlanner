using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HierarchyWorldScalePerformance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_project_spaces_project_workspace_id_order_key_id",
                table: "project_spaces");

            migrationBuilder.DropIndex(
                name: "IX_project_folders_project_space_id",
                table: "project_folders");

            migrationBuilder.DropIndex(
                name: "IX_project_folders_project_workspace_id",
                table: "project_folders");

            migrationBuilder.DropIndex(
                name: "IX_project_folders_project_workspace_id_project_space_id_order~",
                table: "project_folders");

            migrationBuilder.CreateIndex(
                name: "IX_project_spaces_project_workspace_id_order_key_id",
                table: "project_spaces",
                columns: new[] { "project_workspace_id", "order_key", "id" },
                filter: "\"deleted_at\" IS NULL AND \"is_archived\" = false")
                .Annotation("Npgsql:IndexInclude", new[] { "name", "is_private" });

            migrationBuilder.CreateIndex(
                name: "IX_project_folders_project_workspace_id_project_space_id_order~",
                table: "project_folders",
                columns: new[] { "project_workspace_id", "project_space_id", "order_key", "id" },
                filter: "\"deleted_at\" IS NULL AND \"is_archived\" = false")
                .Annotation("Npgsql:IndexInclude", new[] { "name", "is_private" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_project_spaces_project_workspace_id_order_key_id",
                table: "project_spaces");

            migrationBuilder.DropIndex(
                name: "IX_project_folders_project_workspace_id_project_space_id_order~",
                table: "project_folders");

            migrationBuilder.CreateIndex(
                name: "IX_project_spaces_project_workspace_id_order_key_id",
                table: "project_spaces",
                columns: new[] { "project_workspace_id", "order_key", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_project_folders_project_space_id",
                table: "project_folders",
                column: "project_space_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_folders_project_workspace_id",
                table: "project_folders",
                column: "project_workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_folders_project_workspace_id_project_space_id_order~",
                table: "project_folders",
                columns: new[] { "project_workspace_id", "project_space_id", "order_key", "id" });
        }
    }
}
