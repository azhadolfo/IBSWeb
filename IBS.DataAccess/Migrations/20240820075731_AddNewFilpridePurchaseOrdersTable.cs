using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddNewFilpridePurchaseOrdersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_purchase_orders_purc",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropTable(
                name: "filpride_purchase_orders");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_customer_order_slips_purchase_orders_purchase_orde",
                table: "filpride_customer_order_slips",
                column: "purchase_order_id",
                principalTable: "purchase_orders",
                principalColumn: "purchase_order_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_customer_order_slips_purchase_orders_purchase_orde",
                table: "filpride_customer_order_slips");

            migrationBuilder.CreateTable(
                name: "filpride_purchase_orders",
                columns: table => new
                {
                    purchase_order_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    is_served = table.Column<bool>(type: "boolean", nullable: false),
                    port = table.Column<int>(type: "integer", nullable: false),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    purchase_order_no = table.Column<string>(type: "varchar(12)", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    quantity_served = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    remarks = table.Column<string>(type: "varchar(200)", nullable: false),
                    served_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    terms = table.Column<string>(type: "varchar(5)", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_purchase_orders", x => x.purchase_order_id);
                    table.ForeignKey(
                        name: "fk_filpride_purchase_orders_filpride_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "filpride_suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_purchase_orders_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_filpride_purchase_orders_date",
                table: "filpride_purchase_orders",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_purchase_orders_product_id",
                table: "filpride_purchase_orders",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_purchase_orders_purchase_order_no",
                table: "filpride_purchase_orders",
                column: "purchase_order_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_purchase_orders_supplier_id",
                table: "filpride_purchase_orders",
                column: "supplier_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_purchase_orders_purc",
                table: "filpride_customer_order_slips",
                column: "purchase_order_id",
                principalTable: "filpride_purchase_orders",
                principalColumn: "purchase_order_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
