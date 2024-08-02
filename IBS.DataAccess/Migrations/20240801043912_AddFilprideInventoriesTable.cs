using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddFilprideInventoriesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "filpride_inventories",
                columns: table => new
                {
                    inventory_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    particular = table.Column<string>(type: "varchar(200)", nullable: false),
                    reference = table.Column<string>(type: "varchar(12)", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    inventory_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    average_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit = table.Column<string>(type: "varchar(2)", nullable: false),
                    is_validated = table.Column<bool>(type: "boolean", nullable: false),
                    validated_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    validated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    po_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_inventories", x => x.inventory_id);
                    table.ForeignKey(
                        name: "fk_filpride_inventories_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_filpride_inventories_purchase_orders_po_id",
                        column: x => x.po_id,
                        principalTable: "purchase_orders",
                        principalColumn: "purchase_order_id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_filpride_inventories_po_id",
                table: "filpride_inventories",
                column: "po_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_inventories_product_id",
                table: "filpride_inventories",
                column: "product_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "filpride_inventories");
        }
    }
}
