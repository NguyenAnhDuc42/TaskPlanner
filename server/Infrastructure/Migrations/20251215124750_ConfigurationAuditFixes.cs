using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConfigurationAuditFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "sessions");

            migrationBuilder.RenameColumn(
                name: "row_version",
                table: "workspace_members",
                newName: "version");

            migrationBuilder.RenameColumn(
                name: "row_version",
                table: "task_assignments",
                newName: "version");

            migrationBuilder.RenameColumn(
                name: "UserAgent",
                table: "sessions",
                newName: "user_agent");

            migrationBuilder.RenameColumn(
                name: "RevokedAt",
                table: "sessions",
                newName: "revoked_at");

            migrationBuilder.RenameColumn(
                name: "IpAddress",
                table: "sessions",
                newName: "ip_address");

            migrationBuilder.RenameColumn(
                name: "row_version",
                table: "entity_members",
                newName: "version");

            migrationBuilder.RenameColumn(
                name: "row_version",
                table: "chat_room_members",
                newName: "version");

            migrationBuilder.RenameColumn(
                name: "row_version",
                table: "attachment_links",
                newName: "version");

            migrationBuilder.AddColumn<string>(
                name: "AuthProvider",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "user_agent",
                table: "sessions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ip_address",
                table: "sessions",
                type: "character varying(45)",
                maxLength: 45,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_token_rotation_at",
                table: "sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "password_reset_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_used = table.Column<bool>(type: "boolean", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_password_reset_tokens", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sessions_user_id_revoked_at_expires_at",
                table: "sessions",
                columns: new[] { "user_id", "revoked_at", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_token",
                table: "password_reset_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_user_id",
                table: "password_reset_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_user_id_is_used_expires_at",
                table: "password_reset_tokens",
                columns: new[] { "user_id", "is_used", "expires_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "password_reset_tokens");

            migrationBuilder.DropIndex(
                name: "IX_sessions_user_id_revoked_at_expires_at",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "AuthProvider",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "last_token_rotation_at",
                table: "sessions");

            migrationBuilder.RenameColumn(
                name: "version",
                table: "workspace_members",
                newName: "row_version");

            migrationBuilder.RenameColumn(
                name: "version",
                table: "task_assignments",
                newName: "row_version");

            migrationBuilder.RenameColumn(
                name: "user_agent",
                table: "sessions",
                newName: "UserAgent");

            migrationBuilder.RenameColumn(
                name: "revoked_at",
                table: "sessions",
                newName: "RevokedAt");

            migrationBuilder.RenameColumn(
                name: "ip_address",
                table: "sessions",
                newName: "IpAddress");

            migrationBuilder.RenameColumn(
                name: "version",
                table: "entity_members",
                newName: "row_version");

            migrationBuilder.RenameColumn(
                name: "version",
                table: "chat_room_members",
                newName: "row_version");

            migrationBuilder.RenameColumn(
                name: "version",
                table: "attachment_links",
                newName: "row_version");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatorId",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "sessions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "sessions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(45)",
                oldMaxLength: 45);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatorId",
                table: "sessions",
                type: "uuid",
                nullable: true);
        }
    }
}
