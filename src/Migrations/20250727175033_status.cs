using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace src.Migrations
{
    public partial class status : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Remove the old placeholder column
            migrationBuilder.DropColumn(
                name: "MyProperty",
                table: "Tasks");

            // 2) Add JoinCode as nullable (no default)
            migrationBuilder.AddColumn<string>(
                name: "JoinCode",
                table: "Workspaces",
                type: "text",
                nullable: true);

            // 3) Add StatusId (unchanged)
            migrationBuilder.AddColumn<Guid>(
                name: "StatusId",
                table: "Tasks",
                type: "uuid",
                nullable: true);

            // 4) Create Statuses table (unchanged)
            migrationBuilder.CreateTable(
                name: "Statuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Statuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Statuses_Spaces_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "Spaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

                            // 5) Back‑fill unique JoinCode values
                migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    rec RECORD;
                    charset TEXT := 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
                    code TEXT;
                BEGIN
                    FOR rec IN SELECT ""Id"" FROM ""Workspaces"" LOOP
                        code := (
                            SELECT string_agg(
                                    substr(charset, floor(random()*length(charset)+1)::int, 1),
                                    ''
                                )
                            FROM generate_series(1,6)
                        );
                        UPDATE ""Workspaces""
                        SET ""JoinCode"" = code
                        WHERE ""Id"" = rec.""Id"";
                    END LOOP;
                END;
                $$;
                ");

            // 6) Alter JoinCode to be NOT NULL
            migrationBuilder.AlterColumn<string>(
                name: "JoinCode",
                table: "Workspaces",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            // 7) Create the unique index on JoinCode
            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_JoinCode",
                table: "Workspaces",
                column: "JoinCode",
                unique: true);

            // 8) Create other indexes and FKs for Statuses
            migrationBuilder.CreateIndex(
                name: "IX_Tasks_StatusId",
                table: "Tasks",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Statuses_SpaceId",
                table: "Statuses",
                column: "SpaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Statuses_StatusId",
                table: "Tasks",
                column: "StatusId",
                principalTable: "Statuses",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop FKs and tables
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Statuses_StatusId",
                table: "Tasks");

            migrationBuilder.DropTable(
                name: "Statuses");

            // Drop the unique index and column
            migrationBuilder.DropIndex(
                name: "IX_Workspaces_JoinCode",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "JoinCode",
                table: "Workspaces");

            // Drop StatusId and restore Tasks.MyProperty
            migrationBuilder.DropIndex(
                name: "IX_Tasks_StatusId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "StatusId",
                table: "Tasks");

            migrationBuilder.AddColumn<Guid>(
                name: "MyProperty",
                table: "Tasks",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);
        }
    }
}
