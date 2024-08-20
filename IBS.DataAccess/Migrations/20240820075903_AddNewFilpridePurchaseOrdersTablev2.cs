using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddNewFilpridePurchaseOrdersTablev2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_customer_order_slips_purchase_orders_purchase_orde",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_inventories_purchase_orders_po_id",
                table: "filpride_inventories");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_sales_invoices_purchase_orders_purchase_order_id",
                table: "filpride_sales_invoices");

            migrationBuilder.DropForeignKey(
                name: "fk_purchase_orders_filpride_suppliers_supplier_id",
                table: "purchase_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_purchase_orders_products_product_id",
                table: "purchase_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_receiving_reports_purchase_orders_po_id",
                table: "receiving_reports");

            migrationBuilder.DropPrimaryKey(
                name: "pk_purchase_orders",
                table: "purchase_orders");

            migrationBuilder.RenameTable(
                name: "purchase_orders",
                newName: "filpride_purchase_orders");

            migrationBuilder.RenameIndex(
                name: "ix_purchase_orders_supplier_id",
                table: "filpride_purchase_orders",
                newName: "ix_filpride_purchase_orders_supplier_id");

            migrationBuilder.RenameIndex(
                name: "ix_purchase_orders_product_id",
                table: "filpride_purchase_orders",
                newName: "ix_filpride_purchase_orders_product_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_filpride_purchase_orders",
                table: "filpride_purchase_orders",
                column: "purchase_order_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_purchase_orders_purc",
                table: "filpride_customer_order_slips",
                column: "purchase_order_id",
                principalTable: "filpride_purchase_orders",
                principalColumn: "purchase_order_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_inventories_filpride_purchase_orders_po_id",
                table: "filpride_inventories",
                column: "po_id",
                principalTable: "filpride_purchase_orders",
                principalColumn: "purchase_order_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_purchase_orders_filpride_suppliers_supplier_id",
                table: "filpride_purchase_orders",
                column: "supplier_id",
                principalTable: "filpride_suppliers",
                principalColumn: "supplier_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_purchase_orders_products_product_id",
                table: "filpride_purchase_orders",
                column: "product_id",
                principalTable: "products",
                principalColumn: "product_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_sales_invoices_filpride_purchase_orders_purchase_o",
                table: "filpride_sales_invoices",
                column: "purchase_order_id",
                principalTable: "filpride_purchase_orders",
                principalColumn: "purchase_order_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_receiving_reports_filpride_purchase_orders_po_id",
                table: "receiving_reports",
                column: "po_id",
                principalTable: "filpride_purchase_orders",
                principalColumn: "purchase_order_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_purchase_orders_purc",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_inventories_filpride_purchase_orders_po_id",
                table: "filpride_inventories");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_purchase_orders_filpride_suppliers_supplier_id",
                table: "filpride_purchase_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_purchase_orders_products_product_id",
                table: "filpride_purchase_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_sales_invoices_filpride_purchase_orders_purchase_o",
                table: "filpride_sales_invoices");

            migrationBuilder.DropForeignKey(
                name: "fk_receiving_reports_filpride_purchase_orders_po_id",
                table: "receiving_reports");

            migrationBuilder.DropPrimaryKey(
                name: "pk_filpride_purchase_orders",
                table: "filpride_purchase_orders");

            migrationBuilder.RenameTable(
                name: "filpride_purchase_orders",
                newName: "purchase_orders");

            migrationBuilder.RenameIndex(
                name: "ix_filpride_purchase_orders_supplier_id",
                table: "purchase_orders",
                newName: "ix_purchase_orders_supplier_id");

            migrationBuilder.RenameIndex(
                name: "ix_filpride_purchase_orders_product_id",
                table: "purchase_orders",
                newName: "ix_purchase_orders_product_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_purchase_orders",
                table: "purchase_orders",
                column: "purchase_order_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_customer_order_slips_purchase_orders_purchase_orde",
                table: "filpride_customer_order_slips",
                column: "purchase_order_id",
                principalTable: "purchase_orders",
                principalColumn: "purchase_order_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_inventories_purchase_orders_po_id",
                table: "filpride_inventories",
                column: "po_id",
                principalTable: "purchase_orders",
                principalColumn: "purchase_order_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_sales_invoices_purchase_orders_purchase_order_id",
                table: "filpride_sales_invoices",
                column: "purchase_order_id",
                principalTable: "purchase_orders",
                principalColumn: "purchase_order_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_orders_filpride_suppliers_supplier_id",
                table: "purchase_orders",
                column: "supplier_id",
                principalTable: "filpride_suppliers",
                principalColumn: "supplier_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_orders_products_product_id",
                table: "purchase_orders",
                column: "product_id",
                principalTable: "products",
                principalColumn: "product_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_receiving_reports_purchase_orders_po_id",
                table: "receiving_reports",
                column: "po_id",
                principalTable: "purchase_orders",
                principalColumn: "purchase_order_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
