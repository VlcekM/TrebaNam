using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrebaNam.API.Migrations
{
    /// <summary>
    /// Skupiny sa stavaju riadkami domacnosti (dovtedy to bol pevny zoznam kodov a jeden stlpec
    /// s poradim) a polozky pribudaju na konkretny zoznam, lebo domacnost si ich vedie viac.
    /// Existujuce domacnosti tym nemaju co stratit: dostanu tych istych sest skupin v poradi,
    /// v akom ich mali, a jeden zoznam so vsetkym, co uz na nom bolo.
    /// </summary>
    public partial class CategoriesAndLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "household_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    household_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    position = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_household_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_household_categories_households_household_id",
                        column: x => x.household_id,
                        principalTable: "households",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shopping_lists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    household_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    note = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: true),
                    position = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shopping_lists", x => x.id);
                    table.ForeignKey(
                        name: "fk_shopping_lists_households_household_id",
                        column: x => x.household_id,
                        principalTable: "households",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_household_categories_household_id_code",
                table: "household_categories",
                columns: new[] { "household_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shopping_lists_household_id",
                table: "shopping_lists",
                column: "household_id");

            // Zakladnych sest skupin kazdej domacnosti, a to v poradi, ktore si ulozila do
            // category_order; co v nom nebolo, ide za ne tak, ako to bolo v appke.
            migrationBuilder.Sql("""
                INSERT INTO household_categories (id, household_id, code, name, position, created_at)
                SELECT
                    gen_random_uuid(),
                    h.id,
                    d.code,
                    NULL,
                    row_number() OVER (
                        PARTITION BY h.id
                        ORDER BY COALESCE(
                            array_position(string_to_array(COALESCE(h.category_order, ''), ','), d.code),
                            100 + d.ord),
                        d.ord) - 1,
                    now()
                FROM households h
                CROSS JOIN (VALUES
                    ('produce', 0), ('bakery', 1), ('dairy', 2),
                    ('pantry', 3), ('household', 4), ('other', 5)) AS d(code, ord);
                """);

            migrationBuilder.DropColumn(
                name: "category_order",
                table: "households");

            // Jeden zoznam na domacnost - presne to, co dovtedy bol jej jediny zoznam. Nazov je
            // po anglicky, lebo databaza o jazyku cloveka nevie; premenovat sa da v appke.
            migrationBuilder.Sql("""
                INSERT INTO shopping_lists (id, household_id, name, color, note, position, created_at)
                SELECT gen_random_uuid(), h.id, 'Shopping list', 'green', NULL, 0, now()
                FROM households h;
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "list_id",
                table: "items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("""
                UPDATE items i SET list_id = l.id
                FROM shopping_lists l
                WHERE l.household_id = i.household_id;
                """);

            migrationBuilder.AddColumn<string>(
                name: "list_color",
                table: "shopping_records",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "list_name",
                table: "shopping_records",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            // Ukonceny nakup nesie odpis zoznamu; tie starsie prisli z toho jedineho, ktory bol.
            migrationBuilder.Sql("""
                UPDATE shopping_records r SET list_name = l.name, list_color = l.color
                FROM shopping_lists l
                WHERE l.household_id = r.household_id;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_items_list_id",
                table: "items",
                column: "list_id");

            migrationBuilder.AddForeignKey(
                name: "fk_items_shopping_lists_list_id",
                table: "items",
                column: "list_id",
                principalTable: "shopping_lists",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "category_order",
                table: "households",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            // Naspat sa zmesti len poradie zakladnych skupin; vlastne skupiny domacnosti sa do
            // jedneho stlpca s kodmi nevojdu a polozky v nich sa potom citaju ako "ostatne".
            migrationBuilder.Sql("""
                UPDATE households h SET category_order = c.codes
                FROM (
                    SELECT household_id, string_agg(code, ',' ORDER BY position) AS codes
                    FROM household_categories
                    WHERE name IS NULL
                    GROUP BY household_id) c
                WHERE c.household_id = h.id;
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_items_shopping_lists_list_id",
                table: "items");

            migrationBuilder.DropTable(
                name: "household_categories");

            migrationBuilder.DropTable(
                name: "shopping_lists");

            migrationBuilder.DropIndex(
                name: "ix_items_list_id",
                table: "items");

            migrationBuilder.DropColumn(
                name: "list_color",
                table: "shopping_records");

            migrationBuilder.DropColumn(
                name: "list_name",
                table: "shopping_records");

            migrationBuilder.DropColumn(
                name: "list_id",
                table: "items");
        }
    }
}
