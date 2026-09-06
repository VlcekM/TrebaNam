using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrebaNam.API.Migrations
{
    /// <inheritdoc />
    public partial class PartialAmounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bought_quantity",
                table: "items",
                type: "character varying(24)",
                maxLength: 24,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bought_quantity",
                table: "items");
        }
    }
}
