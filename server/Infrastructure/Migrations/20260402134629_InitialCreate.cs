using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attachment_links",
                columns: table => new
                {
                    attachment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attachment_links", x => new { x.attachment_id, x.parent_entity_type, x.parent_entity_id });
                });

            migrationBuilder.CreateTable(
                name: "attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    storage_provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    storage_path = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    content_type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    attachment_type = table.Column<int>(type: "integer", nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    checksum = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    checksum_algorithm = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    processing_state = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attachments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "chat_rooms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    project_workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    avatar_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_private = table.Column<bool>(type: "boolean", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_rooms", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    is_edited = table.Column<bool>(type: "boolean", nullable: false),
                    parent_comment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dashboards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    layer_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    layer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_shared = table.Column<bool>(type: "boolean", nullable: false),
                    is_main = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dashboards", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    layer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    layer_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "entity_access",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                name: "notification_deliveries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chanel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    last_retry_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    metadate = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_deliveries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payload = table.Column<string>(type: "text", nullable: true),
                    is_critical = table.Column<bool>(type: "boolean", nullable: false),
                    aggregation_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_aggregated = table.Column<bool>(type: "boolean", nullable: false),
                    aggregated_into_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: true),
                    enabled_event_types = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    notification_frequency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    notification_channels = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_muted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_preferences", x => x.id);
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
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_password_reset_tokens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "project_folders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_space_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    custom_color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    custom_icon = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    order_key = table.Column<long>(type: "bigint", nullable: false),
                    is_private = table.Column<bool>(type: "boolean", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    next_item_order = table.Column<long>(type: "bigint", nullable: false),
                    start_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    due_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_folders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "project_spaces",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    custom_color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    custom_icon = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_private = table.Column<bool>(type: "boolean", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    order_key = table.Column<long>(type: "bigint", nullable: false),
                    next_item_order = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_spaces", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "project_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_space_id = table.Column<Guid>(type: "uuid", nullable: true),
                    project_folder_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    custom_color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    custom_icon = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    start_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    due_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    story_points = table.Column<int>(type: "integer", nullable: true),
                    time_estimate = table.Column<long>(type: "bigint", nullable: true),
                    order_key = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_tasks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "project_workspaces",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    join_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    custom_color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    custom_icon = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    theme = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    variant = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    strict_join = table.Column<bool>(type: "boolean", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    next_item_order = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_workspaces", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    refresh_token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    last_token_rotation_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_notifications",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    chanel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_notifications", x => new { x.user_id, x.notification_event_id });
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    auth_provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    external_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "view_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    layer_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    layer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    view_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    filter_config_json = table.Column<string>(type: "text", nullable: true),
                    display_config_json = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_view_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    chat_room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    is_edited = table.Column<bool>(type: "boolean", nullable: false),
                    edited_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_pinned = table.Column<bool>(type: "boolean", nullable: false),
                    has_attachment = table.Column<bool>(type: "boolean", nullable: false),
                    reply_to_message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reaction_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_chat_messages_chat_messages_reply_to_message_id",
                        column: x => x.reply_to_message_id,
                        principalTable: "chat_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chat_messages_chat_rooms_chat_room_id",
                        column: x => x.chat_room_id,
                        principalTable: "chat_rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_room_members",
                columns: table => new
                {
                    chat_room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_muted = table.Column<bool>(type: "boolean", nullable: false),
                    mute_end_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_banned = table.Column<bool>(type: "boolean", nullable: false),
                    banned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    banned_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_room_members", x => new { x.chat_room_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_chat_room_members_chat_rooms_chat_room_id",
                        column: x => x.chat_room_id,
                        principalTable: "chat_rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "widgets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    DashboardId = table.Column<Guid>(type: "uuid", nullable: false),
                    layout_col = table.Column<int>(type: "integer", nullable: false),
                    layout_row = table.Column<int>(type: "integer", nullable: false),
                    layout_width = table.Column<int>(type: "integer", nullable: false),
                    layout_height = table.Column<int>(type: "integer", nullable: false),
                    layer_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    layer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    widget_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    config_json = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_widgets", x => x.id);
                    table.ForeignKey(
                        name: "FK_widgets_dashboards_DashboardId",
                        column: x => x.DashboardId,
                        principalTable: "dashboards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_space_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflows", x => x.id);
                    table.ForeignKey(
                        name: "FK_workflows_project_spaces_project_space_id",
                        column: x => x.project_space_id,
                        principalTable: "project_spaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workspace_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_pinned = table.Column<bool>(type: "boolean", nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    suspended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    suspended_by = table.Column<Guid>(type: "uuid", nullable: true),
                    join_method = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspace_members", x => x.id);
                    table.ForeignKey(
                        name: "FK_workspace_members_project_workspaces_project_workspace_id",
                        column: x => x.project_workspace_id,
                        principalTable: "project_workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workspace_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "statuses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_default_status = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_statuses", x => x.id);
                    table.ForeignKey(
                        name: "FK_statuses_workflows_workflow_id",
                        column: x => x.workflow_id,
                        principalTable: "workflows",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estimated_hours = table.Column<int>(type: "integer", nullable: true),
                    actual_hours = table.Column<int>(type: "integer", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_task_assignments_project_tasks_project_task_id",
                        column: x => x.project_task_id,
                        principalTable: "project_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_task_assignments_workspace_members_workspace_member_id",
                        column: x => x.workspace_member_id,
                        principalTable: "workspace_members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attachment_links_attachment_id",
                table: "attachment_links",
                column: "attachment_id");

            migrationBuilder.CreateIndex(
                name: "IX_attachment_links_creator_id",
                table: "attachment_links",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_attachment_links_parent_entity_type_parent_entity_id",
                table: "attachment_links",
                columns: new[] { "parent_entity_type", "parent_entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_attachments_attachment_type",
                table: "attachments",
                column: "attachment_type");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_checksum",
                table: "attachments",
                column: "checksum");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_creator_id",
                table: "attachments",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_processing_state",
                table: "attachments",
                column: "processing_state");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_storage_key",
                table: "attachments",
                column: "storage_key");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_chat_room_id",
                table: "chat_messages",
                column: "chat_room_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_creator_id",
                table: "chat_messages",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_reply_to_message_id",
                table: "chat_messages",
                column: "reply_to_message_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_room_members_chat_room_id",
                table: "chat_room_members",
                column: "chat_room_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_room_members_chat_room_id_role",
                table: "chat_room_members",
                columns: new[] { "chat_room_id", "role" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_room_members_creator_id",
                table: "chat_room_members",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_room_members_user_id",
                table: "chat_room_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_rooms_creator_id",
                table: "chat_rooms",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_rooms_project_workspace_id",
                table: "chat_rooms",
                column: "project_workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_comments_creator_id",
                table: "comments",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_comments_project_task_id",
                table: "comments",
                column: "project_task_id");

            migrationBuilder.CreateIndex(
                name: "IX_dashboards_creator_id",
                table: "dashboards",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_dashboards_layer_id",
                table: "dashboards",
                column: "layer_id");

            migrationBuilder.CreateIndex(
                name: "IX_dashboards_layer_type_layer_id",
                table: "dashboards",
                columns: new[] { "layer_type", "layer_id" });

            migrationBuilder.CreateIndex(
                name: "IX_documents_creator_id",
                table: "documents",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_documents_layer_id_layer_type",
                table: "documents",
                columns: new[] { "layer_id", "layer_type" });

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
                name: "IX_entity_access_project_workspace_id",
                table: "entity_access",
                column: "project_workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_entity_access_project_workspace_id_entity_id_entity_layer",
                table: "entity_access",
                columns: new[] { "project_workspace_id", "entity_id", "entity_layer" });

            migrationBuilder.CreateIndex(
                name: "IX_entity_access_workspace_member_id",
                table: "entity_access",
                column: "workspace_member_id");

            migrationBuilder.CreateIndex(
                name: "IX_entity_access_workspace_member_id_entity_id",
                table: "entity_access",
                columns: new[] { "workspace_member_id", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_entity_access_workspace_member_id_entity_id_entity_layer",
                table: "entity_access",
                columns: new[] { "workspace_member_id", "entity_id", "entity_layer" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_creator_id",
                table: "notification_deliveries",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_status",
                table: "notification_deliveries",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_user_notification_id",
                table: "notification_deliveries",
                column: "user_notification_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_events_creator_id",
                table: "notification_events",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_events_event_type",
                table: "notification_events",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "IX_notification_events_source_id_source_type",
                table: "notification_events",
                columns: new[] { "source_id", "source_type" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_events_workspace_id",
                table: "notification_events",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_preferences_creator_id",
                table: "notification_preferences",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_preferences_user_id",
                table: "notification_preferences",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_preferences_user_id_scope_type_scope_id",
                table: "notification_preferences",
                columns: new[] { "user_id", "scope_type", "scope_id" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_project_folders_creator_id",
                table: "project_folders",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_folders_project_space_id",
                table: "project_folders",
                column: "project_space_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_folders_project_space_id_order_key",
                table: "project_folders",
                columns: new[] { "project_space_id", "order_key" });

            migrationBuilder.CreateIndex(
                name: "IX_project_spaces_creator_id",
                table: "project_spaces",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_spaces_project_workspace_id",
                table: "project_spaces",
                column: "project_workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_spaces_project_workspace_id_is_private",
                table: "project_spaces",
                columns: new[] { "project_workspace_id", "is_private" });

            migrationBuilder.CreateIndex(
                name: "IX_project_tasks_creator_id",
                table: "project_tasks",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_tasks_due_date",
                table: "project_tasks",
                column: "due_date");

            migrationBuilder.CreateIndex(
                name: "IX_project_tasks_project_folder_id",
                table: "project_tasks",
                column: "project_folder_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_tasks_project_space_id",
                table: "project_tasks",
                column: "project_space_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_tasks_project_space_id_status_id",
                table: "project_tasks",
                columns: new[] { "project_space_id", "status_id" });

            migrationBuilder.CreateIndex(
                name: "IX_project_tasks_project_workspace_id",
                table: "project_tasks",
                column: "project_workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_workspaces_creator_id",
                table: "project_workspaces",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_workspaces_join_code",
                table: "project_workspaces",
                column: "join_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_workspaces_name",
                table: "project_workspaces",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_user_id",
                table: "sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_user_id_revoked_at_expires_at",
                table: "sessions",
                columns: new[] { "user_id", "revoked_at", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_statuses_creator_id",
                table: "statuses",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_statuses_workflow_id",
                table: "statuses",
                column: "workflow_id");

            migrationBuilder.CreateIndex(
                name: "IX_task_assignments_creator_id",
                table: "task_assignments",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_task_assignments_project_task_id",
                table: "task_assignments",
                column: "project_task_id");

            migrationBuilder.CreateIndex(
                name: "IX_task_assignments_workspace_member_id",
                table: "task_assignments",
                column: "workspace_member_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_notifications_notification_event_id",
                table: "user_notifications",
                column: "notification_event_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_notifications_status",
                table: "user_notifications",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_user_notifications_user_id",
                table: "user_notifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_view_definitions_creator_id",
                table: "view_definitions",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_view_definitions_layer_id_layer_type",
                table: "view_definitions",
                columns: new[] { "layer_id", "layer_type" });

            migrationBuilder.CreateIndex(
                name: "IX_widgets_creator_id",
                table: "widgets",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_widgets_DashboardId",
                table: "widgets",
                column: "DashboardId");

            migrationBuilder.CreateIndex(
                name: "IX_widgets_layer_id",
                table: "widgets",
                column: "layer_id");

            migrationBuilder.CreateIndex(
                name: "IX_widgets_layer_type",
                table: "widgets",
                column: "layer_type");

            migrationBuilder.CreateIndex(
                name: "IX_widgets_layer_type_layer_id",
                table: "widgets",
                columns: new[] { "layer_type", "layer_id" });

            migrationBuilder.CreateIndex(
                name: "IX_widgets_layer_type_layer_id_widget_type",
                table: "widgets",
                columns: new[] { "layer_type", "layer_id", "widget_type" });

            migrationBuilder.CreateIndex(
                name: "IX_workflows_creator_id",
                table: "workflows",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflows_project_space_id",
                table: "workflows",
                column: "project_space_id");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_members_creator_id",
                table: "workspace_members",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_members_project_workspace_id",
                table: "workspace_members",
                column: "project_workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_members_project_workspace_id_status",
                table: "workspace_members",
                columns: new[] { "project_workspace_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_workspace_members_user_id",
                table: "workspace_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_members_user_id_project_workspace_id",
                table: "workspace_members",
                columns: new[] { "user_id", "project_workspace_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attachment_links");

            migrationBuilder.DropTable(
                name: "attachments");

            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "chat_room_members");

            migrationBuilder.DropTable(
                name: "comments");

            migrationBuilder.DropTable(
                name: "documents");

            migrationBuilder.DropTable(
                name: "entity_access");

            migrationBuilder.DropTable(
                name: "notification_deliveries");

            migrationBuilder.DropTable(
                name: "notification_events");

            migrationBuilder.DropTable(
                name: "notification_preferences");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "password_reset_tokens");

            migrationBuilder.DropTable(
                name: "project_folders");

            migrationBuilder.DropTable(
                name: "sessions");

            migrationBuilder.DropTable(
                name: "statuses");

            migrationBuilder.DropTable(
                name: "task_assignments");

            migrationBuilder.DropTable(
                name: "user_notifications");

            migrationBuilder.DropTable(
                name: "view_definitions");

            migrationBuilder.DropTable(
                name: "widgets");

            migrationBuilder.DropTable(
                name: "chat_rooms");

            migrationBuilder.DropTable(
                name: "workflows");

            migrationBuilder.DropTable(
                name: "project_tasks");

            migrationBuilder.DropTable(
                name: "workspace_members");

            migrationBuilder.DropTable(
                name: "dashboards");

            migrationBuilder.DropTable(
                name: "project_spaces");

            migrationBuilder.DropTable(
                name: "project_workspaces");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
