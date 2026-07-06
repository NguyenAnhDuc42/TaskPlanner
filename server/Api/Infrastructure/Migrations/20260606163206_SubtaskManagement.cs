using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api
{
    /// <inheritdoc />
    public partial class SubtaskManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "parent_task_id",
                table: "project_tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_tasks_parent_task_id",
                table: "project_tasks",
                column: "parent_task_id");

            migrationBuilder.AddForeignKey(
                name: "FK_project_tasks_project_tasks_parent_task_id",
                table: "project_tasks",
                column: "parent_task_id",
                principalTable: "project_tasks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_project_tasks_project_tasks_parent_task_id",
                table: "project_tasks");

            migrationBuilder.DropIndex(
                name: "IX_project_tasks_parent_task_id",
                table: "project_tasks");

            migrationBuilder.DropColumn(
                name: "parent_task_id",
                table: "project_tasks");
        }
    }
}
