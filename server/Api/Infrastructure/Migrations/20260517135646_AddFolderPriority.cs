using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api
{
    /// <inheritdoc />
    public partial class AddFolderPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "priority",
                table: "project_folders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "priority",
                table: "project_folders");
        }
    }
}


