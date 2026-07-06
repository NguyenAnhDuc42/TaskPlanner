using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessAndMemberIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_entity_access_space_member_active",
                table: "entity_access",
                columns: new[] { "project_space_id", "workspace_member_id" },
                filter: "deleted_at IS NULL");

            // Functional index for member cursor pagination which sorts by COALESCE(joined_at, created_at)
            migrationBuilder.Sql(@"
                CREATE INDEX ""IX_workspace_members_workspace_joined_id""
                ON workspace_members (project_workspace_id, COALESCE(joined_at, created_at), id)
                WHERE deleted_at IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_entity_access_space_member_active",
                table: "entity_access");

            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_workspace_members_workspace_joined_id"";");
        }
    }
}
