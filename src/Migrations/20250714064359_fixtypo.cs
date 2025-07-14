using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace src.Migrations
{
    /// <inheritdoc />
    public partial class fixtypo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Spaces_Workspaces_WorkSpaceId",
                table: "Spaces");

            migrationBuilder.RenameColumn(
                name: "WorkSpaceId",
                table: "Spaces",
                newName: "WorkspaceId");

            migrationBuilder.RenameIndex(
                name: "IX_Spaces_WorkSpaceId",
                table: "Spaces",
                newName: "IX_Spaces_WorkspaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Spaces_Workspaces_WorkspaceId",
                table: "Spaces",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Spaces_Workspaces_WorkspaceId",
                table: "Spaces");

            migrationBuilder.RenameColumn(
                name: "WorkspaceId",
                table: "Spaces",
                newName: "WorkSpaceId");

            migrationBuilder.RenameIndex(
                name: "IX_Spaces_WorkspaceId",
                table: "Spaces",
                newName: "IX_Spaces_WorkSpaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Spaces_Workspaces_WorkSpaceId",
                table: "Spaces",
                column: "WorkSpaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
