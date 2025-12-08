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
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
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
                    content_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    storage_provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    storage_path = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    content_type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    checksum = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    checksum_algorithm = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    processing_state = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    link_count = table.Column<int>(type: "integer", nullable: false),
                    custom_meta_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
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
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
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
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
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
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
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
                name: "entity_members",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    layer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    layer_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    access_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    notifications_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entity_members", x => new { x.user_id, x.layer_id, x.layer_type });
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
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
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
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
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
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
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
                    inherit_status = table.Column<bool>(type: "boolean", nullable: false),
                    next_item_order = table.Column<long>(type: "bigint", nullable: false),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
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
                name: "project_lists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_space_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_folder_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    custom_color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    custom_icon = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    order_key = table.Column<long>(type: "bigint", nullable: false),
                    is_private = table.Column<bool>(type: "boolean", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    inherit_status = table.Column<bool>(type: "boolean", nullable: false),
                    next_item_order = table.Column<long>(type: "bigint", nullable: false),
                    start_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    due_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_lists", x => x.id);
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
                    inherit_status = table.Column<bool>(type: "boolean", nullable: false),
                    order_key = table.Column<long>(type: "bigint", nullable: false),
                    next_item_order = table.Column<long>(type: "bigint", nullable: false),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
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
                    project_list_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
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
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
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
                name: "statuses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    layer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    layer_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    order_key = table.Column<long>(type: "bigint", nullable: false),
                    is_default_status = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_statuses", x => x.id);
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
                    Version = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
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
                    password_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
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
                    visibility = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    config_json = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_widgets", x => x.id);
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
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
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
                    notifications_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "workspace_members",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    suspended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    suspended_by = table.Column<Guid>(type: "uuid", nullable: true),
                    join_method = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspace_members", x => new { x.project_workspace_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_workspace_members_project_workspaces_project_workspace_id",
                        column: x => x.project_workspace_id,
                        principalTable: "project_workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    refresh_token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_sessions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_assignments",
                columns: table => new
                {
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_assignments", x => new { x.task_id, x.assignee_id });
                    table.ForeignKey(
                        name: "FK_task_assignments_project_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: "project_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_task_assignments_users_assignee_id",
                        column: x => x.assignee_id,
                        principalTable: "users",
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
                name: "IX_attachments_content_id",
                table: "attachments",
                column: "content_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attachments_creator_id",
                table: "attachments",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_link_count",
                table: "attachments",
                column: "link_count");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_processing_state",
                table: "attachments",
                column: "processing_state");

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
                name: "IX_project_lists_creator_id",
                table: "project_lists",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_lists_project_space_id",
                table: "project_lists",
                column: "project_space_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_lists_project_space_id_project_folder_id",
                table: "project_lists",
                columns: new[] { "project_space_id", "project_folder_id" });

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
                name: "IX_project_tasks_project_list_id",
                table: "project_tasks",
                column: "project_list_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_tasks_project_list_id_order_key",
                table: "project_tasks",
                columns: new[] { "project_list_id", "order_key" });

            migrationBuilder.CreateIndex(
                name: "IX_project_tasks_project_list_id_status_id",
                table: "project_tasks",
                columns: new[] { "project_list_id", "status_id" });

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
                name: "IX_statuses_creator_id",
                table: "statuses",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_statuses_layer_id_layer_type",
                table: "statuses",
                columns: new[] { "layer_id", "layer_type" });

            migrationBuilder.CreateIndex(
                name: "IX_statuses_layer_id_layer_type_order_key",
                table: "statuses",
                columns: new[] { "layer_id", "layer_type", "order_key" });

            migrationBuilder.CreateIndex(
                name: "IX_task_assignments_assignee_id",
                table: "task_assignments",
                column: "assignee_id");

            migrationBuilder.CreateIndex(
                name: "IX_task_assignments_creator_id",
                table: "task_assignments",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "IX_task_assignments_task_id",
                table: "task_assignments",
                column: "task_id");

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
                name: "IX_widgets_creator_id",
                table: "widgets",
                column: "creator_id");

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
                name: "dashboards");

            migrationBuilder.DropTable(
                name: "entity_members");

            migrationBuilder.DropTable(
                name: "notification_deliveries");

            migrationBuilder.DropTable(
                name: "notification_events");

            migrationBuilder.DropTable(
                name: "notification_preferences");

            migrationBuilder.DropTable(
                name: "project_folders");

            migrationBuilder.DropTable(
                name: "project_lists");

            migrationBuilder.DropTable(
                name: "project_spaces");

            migrationBuilder.DropTable(
                name: "sessions");

            migrationBuilder.DropTable(
                name: "statuses");

            migrationBuilder.DropTable(
                name: "task_assignments");

            migrationBuilder.DropTable(
                name: "user_notifications");

            migrationBuilder.DropTable(
                name: "widgets");

            migrationBuilder.DropTable(
                name: "workspace_members");

            migrationBuilder.DropTable(
                name: "chat_rooms");

            migrationBuilder.DropTable(
                name: "project_tasks");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "project_workspaces");
        }
    }
}
