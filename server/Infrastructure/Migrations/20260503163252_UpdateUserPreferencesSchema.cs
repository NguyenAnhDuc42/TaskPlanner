using System;
using Domain.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserPreferencesSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attachment_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    attachment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_space_id = table.Column<Guid>(type: "uuid", nullable: true),
                    project_folder_id = table.Column<Guid>(type: "uuid", nullable: true),
                    project_task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    comment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attachment_links", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    is_edited = table.Column<bool>(type: "boolean", nullable: false),
                    parent_comment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "entity_access",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_space_id = table.Column<Guid>(type: "uuid", nullable: true),
                    project_folder_id = table.Column<Guid>(type: "uuid", nullable: true),
                    project_task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    access_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entity_access", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "entity_asset_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    project_space_id = table.Column<Guid>(type: "uuid", nullable: true),
                    project_folder_id = table.Column<Guid>(type: "uuid", nullable: true),
                    project_task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    comment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    project_workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entity_asset_links", x => x.id);
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
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_password_reset_tokens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "project_workspaces",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    join_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    custom_color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    custom_icon = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    strict_join = table.Column<bool>(type: "boolean", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    is_initialized = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_workspaces", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    setting = table.Column<UserSetting>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_preferences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    auth_provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    external_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageKey = table.Column<string>(type: "text", nullable: false),
                    StorageProvider = table.Column<string>(type: "text", nullable: false),
                    StoragePath = table.Column<string>(type: "text", nullable: false),
                    file_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    content_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    attachment_type = table.Column<string>(type: "text", nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    checksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    checksum_algorithm = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "SHA256"),
                    processing_state = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    project_workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attachments", x => x.id);
                    table.ForeignKey(
                        name: "FK_attachments_project_workspaces_project_workspace_id",
                        column: x => x.project_workspace_id,
                        principalTable: "project_workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_blocks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "jsonb", nullable: false),
                    order_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    project_workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_blocks", x => x.id);
                    table.ForeignKey(
                        name: "FK_document_blocks_project_workspaces_project_workspace_id",
                        column: x => x.project_workspace_id,
                        principalTable: "project_workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    project_workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_documents_project_workspaces_project_workspace_id",
                        column: x => x.project_workspace_id,
                        principalTable: "project_workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "view_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_space_id = table.Column<Guid>(type: "uuid", nullable: true),
                    project_folder_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    view_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    order_key = table.Column<string>(type: "text", nullable: false),
                    filter_config_json = table.Column<string>(type: "jsonb", nullable: false),
                    display_config_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    project_workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_view_definitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_view_definitions_project_workspaces_project_workspace_id",
                        column: x => x.project_workspace_id,
                        principalTable: "project_workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_space_id = table.Column<Guid>(type: "uuid", nullable: true),
                    project_folder_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    project_workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflows", x => x.id);
                    table.ForeignKey(
                        name: "FK_workflows_project_workspaces_project_workspace_id",
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
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    last_token_rotation_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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
                name: "workspace_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    suspended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    suspended_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_pinned = table.Column<bool>(type: "boolean", nullable: false),
                    theme = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Dark"),
                    join_method = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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
                    order_key = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    project_workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_statuses", x => x.id);
                    table.ForeignKey(
                        name: "FK_statuses_project_workspaces_project_workspace_id",
                        column: x => x.project_workspace_id,
                        principalTable: "project_workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_statuses_workflows_workflow_id",
                        column: x => x.workflow_id,
                        principalTable: "workflows",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_spaces",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    custom_color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    custom_icon = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_private = table.Column<bool>(type: "boolean", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    order_key = table.Column<string>(type: "text", nullable: false),
                    workflow_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status_id = table.Column<Guid>(type: "uuid", nullable: true),
                    default_document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    due_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    project_workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_spaces", x => x.id);
                    table.ForeignKey(
                        name: "FK_project_spaces_project_workspaces_project_workspace_id",
                        column: x => x.project_workspace_id,
                        principalTable: "project_workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_spaces_statuses_status_id",
                        column: x => x.status_id,
                        principalTable: "statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_project_spaces_workflows_workflow_id",
                        column: x => x.workflow_id,
                        principalTable: "workflows",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "project_folders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_space_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    default_document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    custom_color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    custom_icon = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    order_key = table.Column<string>(type: "text", nullable: false),
                    is_private = table.Column<bool>(type: "boolean", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    start_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    due_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    workflow_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    project_workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_folders", x => x.id);
                    table.ForeignKey(
                        name: "FK_project_folders_project_spaces_project_space_id",
                        column: x => x.project_space_id,
                        principalTable: "project_spaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_folders_project_workspaces_project_workspace_id",
                        column: x => x.project_workspace_id,
                        principalTable: "project_workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_folders_statuses_status_id",
                        column: x => x.status_id,
                        principalTable: "statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_project_folders_workflows_workflow_id",
                        column: x => x.workflow_id,
                        principalTable: "workflows",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "project_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_space_id = table.Column<Guid>(type: "uuid", nullable: true),
                    project_folder_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    default_document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    custom_color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    custom_icon = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    status_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    priority = table.Column<string>(type: "text", nullable: false),
                    start_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    due_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    story_points = table.Column<int>(type: "integer", nullable: true),
                    time_estimate_seconds = table.Column<long>(type: "bigint", nullable: true),
                    order_key = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    project_workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_tasks", x => x.id);
                    table.ForeignKey(
                        name: "FK_project_tasks_project_folders_project_folder_id",
                        column: x => x.project_folder_id,
                        principalTable: "project_folders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_project_tasks_project_spaces_project_space_id",
                        column: x => x.project_space_id,
                        principalTable: "project_spaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_project_tasks_project_workspaces_project_workspace_id",
                        column: x => x.project_workspace_id,
                        principalTable: "project_workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_tasks_statuses_status_id",
                        column: x => x.status_id,
                        principalTable: "statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
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
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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
                name: "IX_attachment_links_comment_id",
                table: "attachment_links",
                column: "comment_id");

            migrationBuilder.CreateIndex(
                name: "IX_attachment_links_project_folder_id",
                table: "attachment_links",
                column: "project_folder_id");

            migrationBuilder.CreateIndex(
                name: "IX_attachment_links_project_space_id",
                table: "attachment_links",
                column: "project_space_id");

            migrationBuilder.CreateIndex(
                name: "IX_attachment_links_project_task_id",
                table: "attachment_links",
                column: "project_task_id");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_attachment_type",
                table: "attachments",
                column: "attachment_type");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_checksum",
                table: "attachments",
                column: "checksum");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_processing_state",
                table: "attachments",
                column: "processing_state");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_project_workspace_id",
                table: "attachments",
                column: "project_workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_StorageKey",
                table: "attachments",
                column: "StorageKey");

            migrationBuilder.CreateIndex(
                name: "IX_comments_project_task_id",
                table: "comments",
                column: "project_task_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_blocks_project_workspace_id",
                table: "document_blocks",
                column: "project_workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_documents_project_workspace_id",
                table: "documents",
                column: "project_workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_entity_access_project_folder_id",
                table: "entity_access",
                column: "project_folder_id");

            migrationBuilder.CreateIndex(
                name: "IX_entity_access_project_space_id",
                table: "entity_access",
                column: "project_space_id");

            migrationBuilder.CreateIndex(
                name: "IX_entity_access_project_task_id",
                table: "entity_access",
                column: "project_task_id");

            migrationBuilder.CreateIndex(
                name: "IX_entity_access_project_workspace_id",
                table: "entity_access",
                column: "project_workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_entity_access_workspace_member_id",
                table: "entity_access",
                column: "workspace_member_id");

            migrationBuilder.CreateIndex(
                name: "IX_entity_asset_links_asset_id",
                table: "entity_asset_links",
                column: "asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_entity_asset_links_comment_id",
                table: "entity_asset_links",
                column: "comment_id");

            migrationBuilder.CreateIndex(
                name: "IX_entity_asset_links_project_folder_id",
                table: "entity_asset_links",
                column: "project_folder_id");

            migrationBuilder.CreateIndex(
                name: "IX_entity_asset_links_project_space_id",
                table: "entity_asset_links",
                column: "project_space_id");

            migrationBuilder.CreateIndex(
                name: "IX_entity_asset_links_project_task_id",
                table: "entity_asset_links",
                column: "project_task_id");

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
                name: "IX_project_folders_project_space_id",
                table: "project_folders",
                column: "project_space_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_folders_project_space_id_slug",
                table: "project_folders",
                columns: new[] { "project_space_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_folders_project_workspace_id",
                table: "project_folders",
                column: "project_workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_folders_project_workspace_id_project_space_id_order~",
                table: "project_folders",
                columns: new[] { "project_workspace_id", "project_space_id", "order_key", "id" },
                filter: "\"deleted_at\" IS NULL AND \"is_archived\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_project_folders_status_id",
                table: "project_folders",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_folders_workflow_id",
                table: "project_folders",
                column: "workflow_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_spaces_project_workspace_id",
                table: "project_spaces",
                column: "project_workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_spaces_project_workspace_id_slug",
                table: "project_spaces",
                columns: new[] { "project_workspace_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_spaces_status_id",
                table: "project_spaces",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_spaces_workflow_id",
                table: "project_spaces",
                column: "workflow_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_tasks_project_folder_id",
                table: "project_tasks",
                column: "project_folder_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_tasks_project_space_id",
                table: "project_tasks",
                column: "project_space_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_tasks_project_workspace_id",
                table: "project_tasks",
                column: "project_workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_tasks_status_id",
                table: "project_tasks",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_workspaces_slug",
                table: "project_workspaces",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sessions_user_id",
                table: "sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_user_id_revoked_at_expires_at",
                table: "sessions",
                columns: new[] { "user_id", "revoked_at", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_statuses_project_workspace_id",
                table: "statuses",
                column: "project_workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_statuses_workflow_id",
                table: "statuses",
                column: "workflow_id");

            migrationBuilder.CreateIndex(
                name: "IX_statuses_workflow_id_order_key",
                table: "statuses",
                columns: new[] { "workflow_id", "order_key" });

            migrationBuilder.CreateIndex(
                name: "IX_task_assignments_project_task_id",
                table: "task_assignments",
                column: "project_task_id");

            migrationBuilder.CreateIndex(
                name: "IX_task_assignments_workspace_member_id",
                table: "task_assignments",
                column: "workspace_member_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_preferences_user_id",
                table: "user_preferences",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_auth_provider_external_id",
                table: "users",
                columns: new[] { "auth_provider", "external_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_view_definitions_project_folder_id",
                table: "view_definitions",
                column: "project_folder_id");

            migrationBuilder.CreateIndex(
                name: "IX_view_definitions_project_space_id",
                table: "view_definitions",
                column: "project_space_id");

            migrationBuilder.CreateIndex(
                name: "IX_view_definitions_project_workspace_id",
                table: "view_definitions",
                column: "project_workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflows_project_workspace_id",
                table: "workflows",
                column: "project_workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_members_project_workspace_id",
                table: "workspace_members",
                column: "project_workspace_id");

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
                name: "comments");

            migrationBuilder.DropTable(
                name: "document_blocks");

            migrationBuilder.DropTable(
                name: "documents");

            migrationBuilder.DropTable(
                name: "entity_access");

            migrationBuilder.DropTable(
                name: "entity_asset_links");

            migrationBuilder.DropTable(
                name: "password_reset_tokens");

            migrationBuilder.DropTable(
                name: "sessions");

            migrationBuilder.DropTable(
                name: "task_assignments");

            migrationBuilder.DropTable(
                name: "user_preferences");

            migrationBuilder.DropTable(
                name: "view_definitions");

            migrationBuilder.DropTable(
                name: "project_tasks");

            migrationBuilder.DropTable(
                name: "workspace_members");

            migrationBuilder.DropTable(
                name: "project_folders");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "project_spaces");

            migrationBuilder.DropTable(
                name: "statuses");

            migrationBuilder.DropTable(
                name: "workflows");

            migrationBuilder.DropTable(
                name: "project_workspaces");
        }
    }
}
