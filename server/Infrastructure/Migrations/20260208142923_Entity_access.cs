using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Entity_access : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "entity_members");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "workspace_members",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "user_notifications",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "task_assignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "chat_room_members",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "attachment_links",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "entity_access",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_layer = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    access_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entity_access", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    occurred_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true),
                    state = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_entity_access_creator_id",
                table: "entity_access",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_entity_access_entity_id",
                table: "entity_access",
                column: "entity_id");

            migrationBuilder.CreateIndex(
                name: "IX_entity_access_entity_id_entity_layer",
                table: "entity_access",
                columns: new[] { "entity_id", "entity_layer" });

            migrationBuilder.CreateIndex(
                name: "IX_entity_access_workspace_member_id",
                table: "entity_access",
                column: "workspace_member_id");

            migrationBuilder.CreateIndex(
                name: "IX_entity_access_workspace_member_id_entity_id",
                table: "entity_access",
                columns: new[] { "workspace_member_id", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_creator_id",
                table: "outbox_messages",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_occurred_on_utc",
                table: "outbox_messages",
                column: "occurred_on_utc");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_state",
                table: "outbox_messages",
                column: "state");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "entity_access");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "workspace_members");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "user_notifications");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "task_assignments");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "chat_room_members");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "attachment_links");

            migrationBuilder.CreateTable(
                name: "entity_members",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    layer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    layer_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    access_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    notifications_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entity_members", x => new { x.user_id, x.layer_id, x.layer_type });
                });

            migrationBuilder.CreateIndex(
                name: "IX_entity_members_creator_id",
                table: "entity_members",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_entity_members_layer_id",
                table: "entity_members",
                column: "layer_id");

            migrationBuilder.CreateIndex(
                name: "IX_entity_members_layer_id_access_level",
                table: "entity_members",
                columns: new[] { "layer_id", "access_level" });

            migrationBuilder.CreateIndex(
                name: "IX_entity_members_layer_id_layer_type",
                table: "entity_members",
                columns: new[] { "layer_id", "layer_type" });

            migrationBuilder.CreateIndex(
                name: "IX_entity_members_user_id",
                table: "entity_members",
                column: "user_id");
        }
    }
}
