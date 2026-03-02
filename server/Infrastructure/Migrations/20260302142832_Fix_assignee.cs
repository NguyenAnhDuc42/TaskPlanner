using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fix_assignee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_task_assignments_users_assignee_id",
                table: "task_assignments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_workspace_members",
                table: "workspace_members");

            migrationBuilder.DropPrimaryKey(
                name: "PK_task_assignments",
                table: "task_assignments");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "task_assignments",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "assignee_id",
                table: "task_assignments",
                newName: "workspace_member_id");

            migrationBuilder.RenameIndex(
                name: "IX_task_assignments_assignee_id",
                table: "task_assignments",
                newName: "IX_task_assignments_workspace_member_id");

            migrationBuilder.AddColumn<int>(
                name: "actual_hours",
                table: "task_assignments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "completed_at",
                table: "task_assignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "estimated_hours",
                table: "task_assignments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notes",
                table: "task_assignments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_workspace_members",
                table: "workspace_members",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_task_assignments",
                table: "task_assignments",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_members_user_id_project_workspace_id",
                table: "workspace_members",
                columns: new[] { "user_id", "project_workspace_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_task_assignments_workspace_members_workspace_member_id",
                table: "task_assignments",
                column: "workspace_member_id",
                principalTable: "workspace_members",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_workspace_members_users_user_id",
                table: "workspace_members",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_task_assignments_workspace_members_workspace_member_id",
                table: "task_assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_workspace_members_users_user_id",
                table: "workspace_members");

            migrationBuilder.DropPrimaryKey(
                name: "PK_workspace_members",
                table: "workspace_members");

            migrationBuilder.DropIndex(
                name: "IX_workspace_members_user_id_project_workspace_id",
                table: "workspace_members");

            migrationBuilder.DropPrimaryKey(
                name: "PK_task_assignments",
                table: "task_assignments");

            migrationBuilder.DropColumn(
                name: "actual_hours",
                table: "task_assignments");

            migrationBuilder.DropColumn(
                name: "completed_at",
                table: "task_assignments");

            migrationBuilder.DropColumn(
                name: "estimated_hours",
                table: "task_assignments");

            migrationBuilder.DropColumn(
                name: "notes",
                table: "task_assignments");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "task_assignments",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "workspace_member_id",
                table: "task_assignments",
                newName: "assignee_id");

            migrationBuilder.RenameIndex(
                name: "IX_task_assignments_workspace_member_id",
                table: "task_assignments",
                newName: "IX_task_assignments_assignee_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_workspace_members",
                table: "workspace_members",
                columns: new[] { "project_workspace_id", "user_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_task_assignments",
                table: "task_assignments",
                columns: new[] { "task_id", "assignee_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_task_assignments_users_assignee_id",
                table: "task_assignments",
                column: "assignee_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
