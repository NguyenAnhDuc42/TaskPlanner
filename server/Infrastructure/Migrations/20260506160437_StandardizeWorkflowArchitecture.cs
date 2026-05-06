using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StandardizeWorkflowArchitecture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_project_folders_workflows_workflow_id",
                table: "project_folders");

            migrationBuilder.DropForeignKey(
                name: "FK_project_spaces_workflows_workflow_id",
                table: "project_spaces");

            migrationBuilder.DropIndex(
                name: "IX_project_spaces_workflow_id",
                table: "project_spaces");

            migrationBuilder.DropIndex(
                name: "IX_project_folders_workflow_id",
                table: "project_folders");

            migrationBuilder.DropColumn(
                name: "workflow_id",
                table: "project_spaces");

            migrationBuilder.DropColumn(
                name: "workflow_id",
                table: "project_folders");

            migrationBuilder.CreateIndex(
                name: "IX_workflows_project_folder_id",
                table: "workflows",
                column: "project_folder_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflows_project_space_id",
                table: "workflows",
                column: "project_space_id");

            migrationBuilder.AddForeignKey(
                name: "FK_workflows_project_folders_project_folder_id",
                table: "workflows",
                column: "project_folder_id",
                principalTable: "project_folders",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_workflows_project_spaces_project_space_id",
                table: "workflows",
                column: "project_space_id",
                principalTable: "project_spaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_workflows_project_folders_project_folder_id",
                table: "workflows");

            migrationBuilder.DropForeignKey(
                name: "FK_workflows_project_spaces_project_space_id",
                table: "workflows");

            migrationBuilder.DropIndex(
                name: "IX_workflows_project_folder_id",
                table: "workflows");

            migrationBuilder.DropIndex(
                name: "IX_workflows_project_space_id",
                table: "workflows");

            migrationBuilder.AddColumn<Guid>(
                name: "workflow_id",
                table: "project_spaces",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "workflow_id",
                table: "project_folders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_spaces_workflow_id",
                table: "project_spaces",
                column: "workflow_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_folders_workflow_id",
                table: "project_folders",
                column: "workflow_id");

            migrationBuilder.AddForeignKey(
                name: "FK_project_folders_workflows_workflow_id",
                table: "project_folders",
                column: "workflow_id",
                principalTable: "workflows",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_project_spaces_workflows_workflow_id",
                table: "project_spaces",
                column: "workflow_id",
                principalTable: "workflows",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
