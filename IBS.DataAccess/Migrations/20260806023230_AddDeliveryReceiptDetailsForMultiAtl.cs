using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryReceiptDetailsForMultiAtl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "filpride_delivery_receipt_details",
                columns: table => new
                {
                    delivery_receipt_detail_id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivery_receipt_id = table.Column<int>(type: "integer", nullable: false),
                    customer_order_slip_id = table.Column<int>(type: "integer", nullable: false),
                    purchase_order_id = table.Column<int>(type: "integer", nullable: false),
                    authority_to_load_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    product_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    authority_to_load_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_delivery_receipt_details", x => x.delivery_receipt_detail_id);
                    table.ForeignKey(
                        name: "fk_filpride_delivery_receipt_details_filpride_authority_to_loa",
                        column: x => x.authority_to_load_id,
                        principalTable: "filpride_authority_to_loads",
                        principalColumn: "authority_to_load_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_delivery_receipt_details_filpride_customer_order_s",
                        column: x => x.customer_order_slip_id,
                        principalTable: "filpride_customer_order_slips",
                        principalColumn: "customer_order_slip_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_delivery_receipt_details_filpride_delivery_receipt",
                        column: x => x.delivery_receipt_id,
                        principalTable: "filpride_delivery_receipts",
                        principalColumn: "delivery_receipt_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_filpride_delivery_receipt_details_filpride_purchase_orders_",
                        column: x => x.purchase_order_id,
                        principalTable: "filpride_purchase_orders",
                        principalColumn: "purchase_order_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_delivery_receipt_details_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_receipt_details_authority_to_load_id",
                table: "filpride_delivery_receipt_details",
                column: "authority_to_load_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_receipt_details_customer_order_slip_id",
                table: "filpride_delivery_receipt_details",
                column: "customer_order_slip_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_receipt_details_delivery_receipt_id",
                table: "filpride_delivery_receipt_details",
                column: "delivery_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_receipt_details_product_id",
                table: "filpride_delivery_receipt_details",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_receipt_details_purchase_order_id",
                table: "filpride_delivery_receipt_details",
                column: "purchase_order_id");

            migrationBuilder.Sql("""
                CREATE EXTENSION IF NOT EXISTS pgcrypto;
                """);

            migrationBuilder.Sql("""
                INSERT INTO filpride_delivery_receipt_details
                (
                    delivery_receipt_detail_id,
                    delivery_receipt_id,
                    customer_order_slip_id,
                    purchase_order_id,
                    authority_to_load_id,
                    product_id,
                    product_name,
                    authority_to_load_no,
                    quantity,
                    unit_price,
                    total_amount
                )
                SELECT
                    gen_random_uuid(),
                    dr.delivery_receipt_id,
                    dr.customer_order_slip_id,
                    dr.purchase_order_id,
                    dr.authority_to_load_id,
                    cos.product_id,
                    cos.product_name,
                    dr.authority_to_load_no,
                    dr.quantity,
                    cos.delivered_price,
                    dr.total_amount
                FROM filpride_delivery_receipts AS dr
                INNER JOIN filpride_customer_order_slips AS cos
                    ON cos.customer_order_slip_id = dr.customer_order_slip_id
                INNER JOIN filpride_purchase_orders AS po
                    ON po.purchase_order_id = dr.purchase_order_id
                INNER JOIN filpride_authority_to_loads AS atl
                    ON atl.authority_to_load_id = dr.authority_to_load_id
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM filpride_delivery_receipt_details AS drd
                    WHERE drd.delivery_receipt_id = dr.delivery_receipt_id
                );
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM filpride_delivery_receipts AS dr
                        WHERE NOT EXISTS (
                            SELECT 1
                            FROM filpride_delivery_receipt_details AS drd
                            WHERE drd.delivery_receipt_id = dr.delivery_receipt_id
                        )
                    ) THEN
                        RAISE EXCEPTION 'Unable to backfill filpride_delivery_receipt_details for one or more legacy delivery receipts. Resolve missing COS/PO/ATL references first.';
                    END IF;
                END
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "filpride_delivery_receipt_details");
        }
    }
}
