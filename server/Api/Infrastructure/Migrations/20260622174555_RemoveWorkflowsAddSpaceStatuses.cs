using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWorkflowsAddSpaceStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop FK from statuses → workflows (so we can manipulate the column)
            migrationBuilder.DropForeignKey(
                name: "FK_statuses_workflows_workflow_id",
                table: "statuses");

            // 2. Add new project_space_id column (nullable for data migration)
            migrationBuilder.AddColumn<Guid>(
                name: "project_space_id",
                table: "statuses",
                type: "uuid",
                nullable: true);

            // 3. Populate project_space_id from workflows.project_space_id
            migrationBuilder.Sql(@"
                UPDATE statuses s
                SET project_space_id = w.project_space_id
                FROM workflows w
                WHERE s.workflow_id = w.id
                  AND w.project_space_id IS NOT NULL;");

            // 4. For folder-workflow statuses, resolve space via folder
            migrationBuilder.Sql(@"
                UPDATE statuses s
                SET project_space_id = f.project_space_id
                FROM workflows w
                INNER JOIN project_folders f ON f.id = w.project_folder_id
                WHERE s.workflow_id = w.id
                  AND s.project_space_id IS NULL
                  AND w.project_folder_id IS NOT NULL
                  AND f.deleted_at IS NULL;");

            // 5. Delete orphaned statuses (no resolvable space)
            migrationBuilder.Sql(@"
                DELETE FROM statuses WHERE project_space_id IS NULL;");

            // 6. Also delete statuses whose resolved space no longer exists
            migrationBuilder.Sql(@"
                DELETE FROM statuses s
                WHERE NOT EXISTS (
                    SELECT 1 FROM project_spaces ps WHERE ps.id = s.project_space_id AND ps.deleted_at IS NULL
                );");

            // 7. Drop old indexes on workflow_id
            migrationBuilder.DropIndex(name: "IX_statuses_workflow_id_order_key", table: "statuses");
            migrationBuilder.DropIndex(name: "IX_statuses_workflow_id", table: "statuses");

            // 8. Drop workflow_id column
            migrationBuilder.DropColumn(name: "workflow_id", table: "statuses");

            // 9. Make project_space_id NOT NULL
            migrationBuilder.AlterColumn<Guid>(
                name: "project_space_id",
                table: "statuses",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // 10. Add FK from statuses → project_spaces
            migrationBuilder.AddForeignKey(
                name: "FK_statuses_project_spaces_project_space_id",
                table: "statuses",
                column: "project_space_id",
                principalTable: "project_spaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            // 11. Add indexes on project_space_id
            migrationBuilder.CreateIndex(
                name: "IX_statuses_project_space_id",
                table: "statuses",
                column: "project_space_id");

            migrationBuilder.CreateIndex(
                name: "IX_statuses_project_space_id_order_key",
                table: "statuses",
                columns: new[] { "project_space_id", "order_key" });

            // 12. Drop workflows table
            migrationBuilder.DropTable(name: "workflows");

            // 13. Session indexes (from EF snapshot diff)
            migrationBuilder.CreateIndex(
                name: "IX_sessions_previous_refresh_token",
                table: "sessions",
                column: "previous_refresh_token");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_refresh_token",
                table: "sessions",
                column: "refresh_token");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_statuses_project_spaces_project_space_id",
                table: "statuses");

            migrationBuilder.DropIndex(name: "IX_sessions_previous_refresh_token", table: "sessions");
            migrationBuilder.DropIndex(name: "IX_sessions_refresh_token", table: "sessions");
            migrationBuilder.DropIndex(name: "IX_statuses_project_space_id_order_key", table: "statuses");
            migrationBuilder.DropIndex(name: "IX_statuses_project_space_id", table: "statuses");

            migrationBuilder.RenameColumn(
                name: "project_space_id",
                table: "statuses",
                newName: "workflow_id");

            migrationBuilder.CreateTable(
                name: "workflows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    project_folder_id = table.Column<Guid>(type: "uuid", nullable: true),
                    project_space_id = table.Column<Guid>(type: "uuid", nullable: true),
                    project_workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflows", x => x.id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_statuses_workflows_workflow_id",
                table: "statuses",
                column: "workflow_id",
                principalTable: "workflows",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.CreateIndex(name: "IX_statuses_workflow_id", table: "statuses", column: "workflow_id");
            migrationBuilder.CreateIndex(name: "IX_statuses_workflow_id_order_key", table: "statuses", columns: new[] { "workflow_id", "order_key" });
        }
    }
}
