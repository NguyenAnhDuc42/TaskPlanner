using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStatusAndScheduleFromSpace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_project_spaces_statuses_status_id",
                table: "project_spaces");

            migrationBuilder.DropIndex(
                name: "IX_project_spaces_status_id",
                table: "project_spaces");

            migrationBuilder.DropColumn(
                name: "due_date",
                table: "project_spaces");

            migrationBuilder.DropColumn(
                name: "start_date",
                table: "project_spaces");

            migrationBuilder.DropColumn(
                name: "status_id",
                table: "project_spaces");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "due_date",
                table: "project_spaces",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "start_date",
                table: "project_spaces",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "status_id",
                table: "project_spaces",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_spaces_status_id",
                table: "project_spaces",
                column: "status_id");

            migrationBuilder.AddForeignKey(
                name: "FK_project_spaces_statuses_status_id",
                table: "project_spaces",
                column: "status_id",
                principalTable: "statuses",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
