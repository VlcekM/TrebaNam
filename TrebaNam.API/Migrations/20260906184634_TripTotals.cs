using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrebaNam.API.Migrations
{
    /// <inheritdoc />
    public partial class TripTotals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "total_cost",
                table: "shopping_records",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "total_cost",
                table: "shopping_records");
        }
    }
}
