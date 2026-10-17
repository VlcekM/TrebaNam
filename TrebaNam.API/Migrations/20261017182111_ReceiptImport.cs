using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrebaNam.API.Migrations
{
    /// <inheritdoc />
    public partial class ReceiptImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "receipt_id",
                table: "shopping_records",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "store_name",
                table: "shopping_records",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_shopping_records_household_id_receipt_id",
                table: "shopping_records",
                columns: new[] { "household_id", "receipt_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_shopping_records_household_id_receipt_id",
                table: "shopping_records");

            migrationBuilder.DropColumn(
                name: "receipt_id",
                table: "shopping_records");

            migrationBuilder.DropColumn(
                name: "store_name",
                table: "shopping_records");
        }
    }
}
