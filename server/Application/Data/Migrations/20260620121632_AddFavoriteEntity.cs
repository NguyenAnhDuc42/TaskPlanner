using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Application.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFavoriteEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "view_definitions");

            migrationBuilder.CreateTable(
                name: "favorites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_layer_type = table.Column<string>(type: "text", nullable: false),
                    order_key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    project_workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_favorites", x => x.id);
                    table.ForeignKey(
                        name: "FK_favorites_project_workspaces_project_workspace_id",
                        column: x => x.project_workspace_id,
                        principalTable: "project_workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_favorites_project_workspace_id",
                table: "favorites",
                column: "project_workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_favorites_workspace_member_id",
                table: "favorites",
                column: "workspace_member_id");

            migrationBuilder.CreateIndex(
                name: "IX_favorites_workspace_member_id_entity_layer_type_entity_id",
                table: "favorites",
                columns: new[] { "workspace_member_id", "entity_layer_type", "entity_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "favorites");

            migrationBuilder.CreateTable(
                name: "view_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    display_config_json = table.Column<string>(type: "jsonb", nullable: true),
                    filter_config_json = table.Column<string>(type: "jsonb", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    order_key = table.Column<string>(type: "text", nullable: false),
                    project_folder_id = table.Column<Guid>(type: "uuid", nullable: true),
                    project_space_id = table.Column<Guid>(type: "uuid", nullable: true),
                    project_workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    view_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
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
        }
    }
}
