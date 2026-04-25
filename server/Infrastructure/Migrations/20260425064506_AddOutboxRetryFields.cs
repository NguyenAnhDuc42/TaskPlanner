using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxRetryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "error_count",
                table: "outbox_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "scheduled_at_utc",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_scheduled_at_utc",
                table: "outbox_messages",
                column: "scheduled_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_scheduled_at_utc",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "error_count",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "scheduled_at_utc",
                table: "outbox_messages");
        }
    }
}
