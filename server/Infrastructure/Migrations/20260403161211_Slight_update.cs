using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Slight_update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_default_status",
                table: "statuses");

            migrationBuilder.DropColumn(
                name: "variant",
                table: "project_workspaces");

            migrationBuilder.AddColumn<Guid>(
                name: "project_workspace_id",
                table: "workflows",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "project_workspace_id",
                table: "statuses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_workflows_project_workspace_id",
                table: "workflows",
                column: "project_workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_statuses_project_workspace_id",
                table: "statuses",
                column: "project_workspace_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_workflows_project_workspace_id",
                table: "workflows");

            migrationBuilder.DropIndex(
                name: "IX_statuses_project_workspace_id",
                table: "statuses");

            migrationBuilder.DropColumn(
                name: "project_workspace_id",
                table: "workflows");

            migrationBuilder.DropColumn(
                name: "project_workspace_id",
                table: "statuses");

            migrationBuilder.AddColumn<bool>(
                name: "is_default_status",
                table: "statuses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "variant",
                table: "project_workspaces",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
