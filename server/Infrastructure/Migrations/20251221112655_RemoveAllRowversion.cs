using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAllRowversion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "version",
                table: "workspace_members");

            migrationBuilder.DropColumn(
                name: "version",
                table: "widgets");

            migrationBuilder.DropColumn(
                name: "version",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "user_notifications");

            migrationBuilder.DropColumn(
                name: "version",
                table: "task_assignments");

            migrationBuilder.DropColumn(
                name: "version",
                table: "statuses");

            migrationBuilder.DropColumn(
                name: "version",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "version",
                table: "project_workspaces");

            migrationBuilder.DropColumn(
                name: "version",
                table: "project_tasks");

            migrationBuilder.DropColumn(
                name: "version",
                table: "project_spaces");

            migrationBuilder.DropColumn(
                name: "version",
                table: "project_lists");

            migrationBuilder.DropColumn(
                name: "version",
                table: "project_folders");

            migrationBuilder.DropColumn(
                name: "version",
                table: "password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "version",
                table: "notification_preferences");

            migrationBuilder.DropColumn(
                name: "version",
                table: "notification_events");

            migrationBuilder.DropColumn(
                name: "version",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "version",
                table: "entity_members");

            migrationBuilder.DropColumn(
                name: "version",
                table: "dashboards");

            migrationBuilder.DropColumn(
                name: "version",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "version",
                table: "chat_rooms");

            migrationBuilder.DropColumn(
                name: "version",
                table: "chat_room_members");

            migrationBuilder.DropColumn(
                name: "version",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "version",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "version",
                table: "attachment_links");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "workspace_members",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "gen_random_bytes(8)");

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "widgets",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "gen_random_bytes(8)");

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "users",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "gen_random_bytes(8)");

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "user_notifications",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "task_assignments",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "gen_random_bytes(8)");

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "statuses",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "gen_random_bytes(8)");

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "sessions",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "gen_random_bytes(8)");

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "project_workspaces",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "gen_random_bytes(8)");

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "project_tasks",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "gen_random_bytes(8)");

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "project_spaces",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "gen_random_bytes(8)");

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "project_lists",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "gen_random_bytes(8)");

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "project_folders",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "gen_random_bytes(8)");

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "password_reset_tokens",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "gen_random_bytes(8)");

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "notification_preferences",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "gen_random_bytes(8)");

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "notification_events",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "gen_random_bytes(8)");

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "notification_deliveries",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "gen_random_bytes(8)");

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "entity_members",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "gen_random_bytes(8)");

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "dashboards",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "gen_random_bytes(8)");

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "comments",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "gen_random_bytes(8)");

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "chat_rooms",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "gen_random_bytes(8)");

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "chat_room_members",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "gen_random_bytes(8)");

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "chat_messages",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "gen_random_bytes(8)");

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "attachments",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "gen_random_bytes(8)");

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "attachment_links",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "gen_random_bytes(8)");
        }
    }
}
