using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentsTree : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_space_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    order_key = table.Column<string>(type: "text", nullable: false),
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
                        name: "FK_documents_documents_parent_document_id",
                        column: x => x.parent_document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_documents_project_spaces_project_space_id",
                        column: x => x.project_space_id,
                        principalTable: "project_spaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_documents_project_workspaces_project_workspace_id",
                        column: x => x.project_workspace_id,
                        principalTable: "project_workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_documents_parent_document_id",
                table: "documents",
                column: "parent_document_id");

            migrationBuilder.CreateIndex(
                name: "IX_documents_project_space_id_parent_document_id_order_key_id",
                table: "documents",
                columns: new[] { "project_space_id", "parent_document_id", "order_key", "id" },
                filter: "\"deleted_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_documents_project_workspace_id",
                table: "documents",
                column: "project_workspace_id");

            // Backfill: every existing Space's DefaultDocumentId becomes the root node of its new
            // Document tree, reusing the same id — so existing DocumentBlock rows (already keyed by
            // that id) automatically become that root document's content with no DocumentBlock
            // migration at all.
            migrationBuilder.Sql(@"
                INSERT INTO documents (id, project_workspace_id, project_space_id, parent_document_id, name, order_key, created_at, updated_at, creator_id, deleted_at)
                SELECT s.default_document_id, s.project_workspace_id, s.id, NULL, s.name, 'a0', s.created_at, s.created_at, s.creator_id, NULL
                FROM project_spaces s
                WHERE s.deleted_at IS NULL
                  AND s.default_document_id != '00000000-0000-0000-0000-000000000000'
                ON CONFLICT (id) DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "documents");
        }
    }
}
