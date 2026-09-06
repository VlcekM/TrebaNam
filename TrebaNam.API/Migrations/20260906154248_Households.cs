using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrebaNam.API.Migrations
{
    /// <inheritdoc />
    public partial class Households : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "household_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "households",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    invite_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_households", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_users_household_id",
                table: "users",
                column: "household_id");

            migrationBuilder.CreateIndex(
                name: "ix_households_invite_code",
                table: "households",
                column: "invite_code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_users_households_household_id",
                table: "users",
                column: "household_id",
                principalTable: "households",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_users_households_household_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "households");

            migrationBuilder.DropIndex(
                name: "ix_users_household_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "household_id",
                table: "users");
        }
    }
}
