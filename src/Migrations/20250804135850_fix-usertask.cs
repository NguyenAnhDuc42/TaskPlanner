using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace src.Migrations
{
    /// <inheritdoc />
    public partial class fixusertask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserTasks_Tasks_TaskId1",
                table: "UserTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_UserTasks_Users_UserId1",
                table: "UserTasks");

            migrationBuilder.DropIndex(
                name: "IX_UserTasks_TaskId1",
                table: "UserTasks");

            migrationBuilder.DropIndex(
                name: "IX_UserTasks_UserId1",
                table: "UserTasks");

            migrationBuilder.DropColumn(
                name: "TaskId1",
                table: "UserTasks");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "UserTasks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TaskId1",
                table: "UserTasks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "UserTasks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_UserTasks_TaskId1",
                table: "UserTasks",
                column: "TaskId1");

            migrationBuilder.CreateIndex(
                name: "IX_UserTasks_UserId1",
                table: "UserTasks",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_UserTasks_Tasks_TaskId1",
                table: "UserTasks",
                column: "TaskId1",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserTasks_Users_UserId1",
                table: "UserTasks",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
