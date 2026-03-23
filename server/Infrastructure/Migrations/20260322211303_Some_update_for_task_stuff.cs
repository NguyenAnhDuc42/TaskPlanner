using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Some_update_for_task_stuff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_task_assignments_project_tasks_task_id",
                table: "task_assignments");

            migrationBuilder.RenameColumn(
                name: "task_id",
                table: "task_assignments",
                newName: "project_task_id");

            migrationBuilder.RenameIndex(
                name: "IX_task_assignments_task_id",
                table: "task_assignments",
                newName: "IX_task_assignments_project_task_id");

            migrationBuilder.CreateIndex(
                name: "IX_widgets_DashboardId",
                table: "widgets",
                column: "DashboardId");

            migrationBuilder.AddForeignKey(
                name: "FK_task_assignments_project_tasks_project_task_id",
                table: "task_assignments",
                column: "project_task_id",
                principalTable: "project_tasks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_widgets_dashboards_DashboardId",
                table: "widgets",
                column: "DashboardId",
                principalTable: "dashboards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_task_assignments_project_tasks_project_task_id",
                table: "task_assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_widgets_dashboards_DashboardId",
                table: "widgets");

            migrationBuilder.DropIndex(
                name: "IX_widgets_DashboardId",
                table: "widgets");

            migrationBuilder.RenameColumn(
                name: "project_task_id",
                table: "task_assignments",
                newName: "task_id");

            migrationBuilder.RenameIndex(
                name: "IX_task_assignments_project_task_id",
                table: "task_assignments",
                newName: "IX_task_assignments_task_id");

            migrationBuilder.AddForeignKey(
                name: "FK_task_assignments_project_tasks_task_id",
                table: "task_assignments",
                column: "task_id",
                principalTable: "project_tasks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
