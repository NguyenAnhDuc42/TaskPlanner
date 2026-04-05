using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FractionalIndexing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_project_folders_project_space_id_order_key",
                table: "project_folders");

            migrationBuilder.DropColumn(
                name: "next_item_order",
                table: "project_workspaces");

            migrationBuilder.DropColumn(
                name: "next_item_order",
                table: "project_spaces");

            migrationBuilder.DropColumn(
                name: "next_item_order",
                table: "project_folders");

            migrationBuilder.AlterColumn<string>(
                name: "order_key",
                table: "project_tasks",
                type: "text",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "order_key",
                table: "project_spaces",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<string>(
                name: "order_key",
                table: "project_folders",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "next_item_order",
                table: "project_workspaces",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<long>(
                name: "order_key",
                table: "project_tasks",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "order_key",
                table: "project_spaces",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<long>(
                name: "next_item_order",
                table: "project_spaces",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<long>(
                name: "order_key",
                table: "project_folders",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<long>(
                name: "next_item_order",
                table: "project_folders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_project_folders_project_space_id_order_key",
                table: "project_folders",
                columns: new[] { "project_space_id", "order_key" });
        }
    }
}
