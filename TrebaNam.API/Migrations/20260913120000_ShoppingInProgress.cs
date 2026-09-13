using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrebaNam.API.Migrations
{
    /// <inheritdoc />
    public partial class ShoppingInProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Prazdne znamena, ze nikto nenakupuje - doterajsie zoznamy teda zacinaju v pokoji.
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "shopping_started_at",
                table: "shopping_lists",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "shopping_started_by_user_id",
                table: "shopping_lists",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "shopping_started_at",
                table: "shopping_lists");

            migrationBuilder.DropColumn(
                name: "shopping_started_by_user_id",
                table: "shopping_lists");
        }
    }
}
