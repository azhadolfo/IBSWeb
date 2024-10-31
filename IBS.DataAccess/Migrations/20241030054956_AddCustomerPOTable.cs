using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerPOTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mobility_customer_purchase_orders",
                columns: table => new
                {
                    customer_purchase_order_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_purchase_order_no = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    address = table.Column<string>(type: "text", nullable: false),
                    station_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_customer_purchase_orders", x => x.customer_purchase_order_id);
                    table.ForeignKey(
                        name: "fk_mobility_customer_purchase_orders_mobility_customers_custom",
                        column: x => x.customer_id,
                        principalTable: "mobility_customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mobility_customer_purchase_orders_mobility_stations_station",
                        column: x => x.station_id,
                        principalTable: "mobility_stations",
                        principalColumn: "station_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mobility_customer_purchase_orders_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mobility_customer_purchase_orders_customer_id",
                table: "mobility_customer_purchase_orders",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_customer_purchase_orders_product_id",
                table: "mobility_customer_purchase_orders",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_customer_purchase_orders_station_id",
                table: "mobility_customer_purchase_orders",
                column: "station_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mobility_customer_purchase_orders");
        }
    }
}
