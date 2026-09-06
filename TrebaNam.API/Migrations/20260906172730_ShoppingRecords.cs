using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrebaNam.API.Migrations
{
    /// <inheritdoc />
    public partial class ShoppingRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "items",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                // Polozky spred kategorii spadaju do "ostatne", nie do prazdneho kodu.
                defaultValue: "other");

            migrationBuilder.AddColumn<bool>(
                name: "is_checked",
                table: "items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "quantity",
                table: "items",
                type: "character varying(24)",
                maxLength: 24,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "shopping_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    household_id = table.Column<Guid>(type: "uuid", nullable: false),
                    completed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shopping_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_shopping_records_households_household_id",
                        column: x => x.household_id,
                        principalTable: "households",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shopping_record_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shopping_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    quantity = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    category = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shopping_record_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_shopping_record_items_shopping_records_shopping_record_id",
                        column: x => x.shopping_record_id,
                        principalTable: "shopping_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_shopping_record_items_shopping_record_id",
                table: "shopping_record_items",
                column: "shopping_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_records_household_id",
                table: "shopping_records",
                column: "household_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shopping_record_items");

            migrationBuilder.DropTable(
                name: "shopping_records");

            migrationBuilder.DropColumn(
                name: "category",
                table: "items");

            migrationBuilder.DropColumn(
                name: "is_checked",
                table: "items");

            migrationBuilder.DropColumn(
                name: "quantity",
                table: "items");
        }
    }
}
