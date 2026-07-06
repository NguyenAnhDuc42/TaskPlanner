using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFolderStatusAndPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_project_folders_statuses_status_id",
                table: "project_folders");

            migrationBuilder.DropIndex(
                name: "IX_project_folders_status_id",
                table: "project_folders");

            migrationBuilder.DropColumn(
                name: "priority",
                table: "project_folders");

            migrationBuilder.DropColumn(
                name: "status_id",
                table: "project_folders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "priority",
                table: "project_folders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "status_id",
                table: "project_folders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_folders_status_id",
                table: "project_folders",
                column: "status_id");

            migrationBuilder.AddForeignKey(
                name: "FK_project_folders_statuses_status_id",
                table: "project_folders",
                column: "status_id",
                principalTable: "statuses",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
