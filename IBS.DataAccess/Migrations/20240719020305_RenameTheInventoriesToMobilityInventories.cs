using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameTheInventoriesToMobilityInventories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "csv_files");

            //migrationBuilder.DropTable(
            //    name: "inventories");

            migrationBuilder.CreateTable(
                name: "mobility_inventories",
                columns: table => new
                {
                    inventory_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    particulars = table.Column<string>(type: "varchar(50)", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    reference = table.Column<string>(type: "varchar(200)", nullable: false),
                    product_code = table.Column<string>(type: "varchar(10)", nullable: false),
                    station_code = table.Column<string>(type: "varchar(10)", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    running_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    inventory_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit_cost_average = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    inventory_value = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    cost_of_goods_sold = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    validated_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    validated_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    transaction_no = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_inventories", x => x.inventory_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mobility_inventories_product_code",
                table: "mobility_inventories",
                column: "product_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_inventories_station_code",
                table: "mobility_inventories",
                column: "station_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_inventories_transaction_no",
                table: "mobility_inventories",
                column: "transaction_no");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mobility_inventories");

            migrationBuilder.CreateTable(
                name: "csv_files",
                columns: table => new
                {
                    file_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    file_name = table.Column<string>(type: "varchar(200)", nullable: false),
                    is_uploaded = table.Column<bool>(type: "boolean", nullable: false),
                    station_code = table.Column<string>(type: "varchar(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_csv_files", x => x.file_id);
                });

            migrationBuilder.CreateTable(
                name: "inventories",
                columns: table => new
                {
                    inventory_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cost_of_goods_sold = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    inventory_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    inventory_value = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    particulars = table.Column<string>(type: "varchar(50)", nullable: false),
                    product_code = table.Column<string>(type: "varchar(10)", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    reference = table.Column<string>(type: "varchar(200)", nullable: false),
                    running_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    station_code = table.Column<string>(type: "varchar(10)", nullable: true),
                    total_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    transaction_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit_cost_average = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    validated_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    validated_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventories", x => x.inventory_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventories_product_code",
                table: "inventories",
                column: "product_code");

            migrationBuilder.CreateIndex(
                name: "ix_inventories_station_code",
                table: "inventories",
                column: "station_code");

            migrationBuilder.CreateIndex(
                name: "ix_inventories_transaction_no",
                table: "inventories",
                column: "transaction_no");
        }
    }
}
