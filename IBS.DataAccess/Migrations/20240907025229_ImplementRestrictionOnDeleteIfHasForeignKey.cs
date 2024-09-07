using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ImplementRestrictionOnDeleteIfHasForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_check_voucher_headers_filpride_bank_accounts_bank_",
                table: "filpride_check_voucher_headers");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_check_voucher_headers_filpride_suppliers_supplier_",
                table: "filpride_check_voucher_headers");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_collection_receipts_filpride_customers_customer_id",
                table: "filpride_collection_receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_collection_receipts_filpride_sales_invoices_sales_",
                table: "filpride_collection_receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_collection_receipts_filpride_service_invoices_serv",
                table: "filpride_collection_receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_credit_memos_filpride_sales_invoices_sales_invoice",
                table: "filpride_credit_memos");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_credit_memos_filpride_service_invoices_service_inv",
                table: "filpride_credit_memos");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_debit_memos_filpride_sales_invoices_sales_invoice_",
                table: "filpride_debit_memos");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_debit_memos_filpride_service_invoices_service_invo",
                table: "filpride_debit_memos");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_journal_voucher_headers_filpride_check_voucher_hea",
                table: "filpride_journal_voucher_headers");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_purchase_orders_filpride_customers_customer_id",
                table: "filpride_purchase_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_purchase_orders_filpride_suppliers_supplier_id",
                table: "filpride_purchase_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_purchase_orders_products_product_id",
                table: "filpride_purchase_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_receiving_reports_filpride_delivery_receipts_deliv",
                table: "filpride_receiving_reports");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_receiving_reports_filpride_purchase_orders_po_id",
                table: "filpride_receiving_reports");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_sales_invoices_filpride_customers_customer_id",
                table: "filpride_sales_invoices");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_sales_invoices_filpride_purchase_orders_purchase_o",
                table: "filpride_sales_invoices");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_sales_invoices_products_product_id",
                table: "filpride_sales_invoices");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_service_invoices_filpride_customers_customer_id",
                table: "filpride_service_invoices");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_service_invoices_filpride_services_service_id",
                table: "filpride_service_invoices");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_check_voucher_headers_filpride_bank_accounts_bank_",
                table: "filpride_check_voucher_headers",
                column: "bank_id",
                principalTable: "filpride_bank_accounts",
                principalColumn: "bank_account_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_check_voucher_headers_filpride_suppliers_supplier_",
                table: "filpride_check_voucher_headers",
                column: "supplier_id",
                principalTable: "filpride_suppliers",
                principalColumn: "supplier_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_collection_receipts_filpride_customers_customer_id",
                table: "filpride_collection_receipts",
                column: "customer_id",
                principalTable: "filpride_customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_collection_receipts_filpride_sales_invoices_sales_",
                table: "filpride_collection_receipts",
                column: "sales_invoice_id",
                principalTable: "filpride_sales_invoices",
                principalColumn: "sales_invoice_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_collection_receipts_filpride_service_invoices_serv",
                table: "filpride_collection_receipts",
                column: "service_invoice_id",
                principalTable: "filpride_service_invoices",
                principalColumn: "service_invoice_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_credit_memos_filpride_sales_invoices_sales_invoice",
                table: "filpride_credit_memos",
                column: "sales_invoice_id",
                principalTable: "filpride_sales_invoices",
                principalColumn: "sales_invoice_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_credit_memos_filpride_service_invoices_service_inv",
                table: "filpride_credit_memos",
                column: "service_invoice_id",
                principalTable: "filpride_service_invoices",
                principalColumn: "service_invoice_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_debit_memos_filpride_sales_invoices_sales_invoice_",
                table: "filpride_debit_memos",
                column: "sales_invoice_id",
                principalTable: "filpride_sales_invoices",
                principalColumn: "sales_invoice_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_debit_memos_filpride_service_invoices_service_invo",
                table: "filpride_debit_memos",
                column: "service_invoice_id",
                principalTable: "filpride_service_invoices",
                principalColumn: "service_invoice_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_journal_voucher_headers_filpride_check_voucher_hea",
                table: "filpride_journal_voucher_headers",
                column: "cv_id",
                principalTable: "filpride_check_voucher_headers",
                principalColumn: "check_voucher_header_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_purchase_orders_filpride_customers_customer_id",
                table: "filpride_purchase_orders",
                column: "customer_id",
                principalTable: "filpride_customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_purchase_orders_filpride_suppliers_supplier_id",
                table: "filpride_purchase_orders",
                column: "supplier_id",
                principalTable: "filpride_suppliers",
                principalColumn: "supplier_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_purchase_orders_products_product_id",
                table: "filpride_purchase_orders",
                column: "product_id",
                principalTable: "products",
                principalColumn: "product_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_receiving_reports_filpride_delivery_receipts_deliv",
                table: "filpride_receiving_reports",
                column: "delivery_receipt_id",
                principalTable: "filpride_delivery_receipts",
                principalColumn: "delivery_receipt_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_receiving_reports_filpride_purchase_orders_po_id",
                table: "filpride_receiving_reports",
                column: "po_id",
                principalTable: "filpride_purchase_orders",
                principalColumn: "purchase_order_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_sales_invoices_filpride_customers_customer_id",
                table: "filpride_sales_invoices",
                column: "customer_id",
                principalTable: "filpride_customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_sales_invoices_filpride_purchase_orders_purchase_o",
                table: "filpride_sales_invoices",
                column: "purchase_order_id",
                principalTable: "filpride_purchase_orders",
                principalColumn: "purchase_order_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_sales_invoices_products_product_id",
                table: "filpride_sales_invoices",
                column: "product_id",
                principalTable: "products",
                principalColumn: "product_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_service_invoices_filpride_customers_customer_id",
                table: "filpride_service_invoices",
                column: "customer_id",
                principalTable: "filpride_customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_service_invoices_filpride_services_service_id",
                table: "filpride_service_invoices",
                column: "service_id",
                principalTable: "filpride_services",
                principalColumn: "service_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_check_voucher_headers_filpride_bank_accounts_bank_",
                table: "filpride_check_voucher_headers");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_check_voucher_headers_filpride_suppliers_supplier_",
                table: "filpride_check_voucher_headers");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_collection_receipts_filpride_customers_customer_id",
                table: "filpride_collection_receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_collection_receipts_filpride_sales_invoices_sales_",
                table: "filpride_collection_receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_collection_receipts_filpride_service_invoices_serv",
                table: "filpride_collection_receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_credit_memos_filpride_sales_invoices_sales_invoice",
                table: "filpride_credit_memos");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_credit_memos_filpride_service_invoices_service_inv",
                table: "filpride_credit_memos");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_debit_memos_filpride_sales_invoices_sales_invoice_",
                table: "filpride_debit_memos");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_debit_memos_filpride_service_invoices_service_invo",
                table: "filpride_debit_memos");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_journal_voucher_headers_filpride_check_voucher_hea",
                table: "filpride_journal_voucher_headers");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_purchase_orders_filpride_customers_customer_id",
                table: "filpride_purchase_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_purchase_orders_filpride_suppliers_supplier_id",
                table: "filpride_purchase_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_purchase_orders_products_product_id",
                table: "filpride_purchase_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_receiving_reports_filpride_delivery_receipts_deliv",
                table: "filpride_receiving_reports");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_receiving_reports_filpride_purchase_orders_po_id",
                table: "filpride_receiving_reports");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_sales_invoices_filpride_customers_customer_id",
                table: "filpride_sales_invoices");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_sales_invoices_filpride_purchase_orders_purchase_o",
                table: "filpride_sales_invoices");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_sales_invoices_products_product_id",
                table: "filpride_sales_invoices");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_service_invoices_filpride_customers_customer_id",
                table: "filpride_service_invoices");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_service_invoices_filpride_services_service_id",
                table: "filpride_service_invoices");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_check_voucher_headers_filpride_bank_accounts_bank_",
                table: "filpride_check_voucher_headers",
                column: "bank_id",
                principalTable: "filpride_bank_accounts",
                principalColumn: "bank_account_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_check_voucher_headers_filpride_suppliers_supplier_",
                table: "filpride_check_voucher_headers",
                column: "supplier_id",
                principalTable: "filpride_suppliers",
                principalColumn: "supplier_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_collection_receipts_filpride_customers_customer_id",
                table: "filpride_collection_receipts",
                column: "customer_id",
                principalTable: "filpride_customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_collection_receipts_filpride_sales_invoices_sales_",
                table: "filpride_collection_receipts",
                column: "sales_invoice_id",
                principalTable: "filpride_sales_invoices",
                principalColumn: "sales_invoice_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_collection_receipts_filpride_service_invoices_serv",
                table: "filpride_collection_receipts",
                column: "service_invoice_id",
                principalTable: "filpride_service_invoices",
                principalColumn: "service_invoice_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_credit_memos_filpride_sales_invoices_sales_invoice",
                table: "filpride_credit_memos",
                column: "sales_invoice_id",
                principalTable: "filpride_sales_invoices",
                principalColumn: "sales_invoice_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_credit_memos_filpride_service_invoices_service_inv",
                table: "filpride_credit_memos",
                column: "service_invoice_id",
                principalTable: "filpride_service_invoices",
                principalColumn: "service_invoice_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_debit_memos_filpride_sales_invoices_sales_invoice_",
                table: "filpride_debit_memos",
                column: "sales_invoice_id",
                principalTable: "filpride_sales_invoices",
                principalColumn: "sales_invoice_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_debit_memos_filpride_service_invoices_service_invo",
                table: "filpride_debit_memos",
                column: "service_invoice_id",
                principalTable: "filpride_service_invoices",
                principalColumn: "service_invoice_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_journal_voucher_headers_filpride_check_voucher_hea",
                table: "filpride_journal_voucher_headers",
                column: "cv_id",
                principalTable: "filpride_check_voucher_headers",
                principalColumn: "check_voucher_header_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_purchase_orders_filpride_customers_customer_id",
                table: "filpride_purchase_orders",
                column: "customer_id",
                principalTable: "filpride_customers",
                principalColumn: "customer_id");

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
                name: "fk_filpride_receiving_reports_filpride_delivery_receipts_deliv",
                table: "filpride_receiving_reports",
                column: "delivery_receipt_id",
                principalTable: "filpride_delivery_receipts",
                principalColumn: "delivery_receipt_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_receiving_reports_filpride_purchase_orders_po_id",
                table: "filpride_receiving_reports",
                column: "po_id",
                principalTable: "filpride_purchase_orders",
                principalColumn: "purchase_order_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_sales_invoices_filpride_customers_customer_id",
                table: "filpride_sales_invoices",
                column: "customer_id",
                principalTable: "filpride_customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_sales_invoices_filpride_purchase_orders_purchase_o",
                table: "filpride_sales_invoices",
                column: "purchase_order_id",
                principalTable: "filpride_purchase_orders",
                principalColumn: "purchase_order_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_sales_invoices_products_product_id",
                table: "filpride_sales_invoices",
                column: "product_id",
                principalTable: "products",
                principalColumn: "product_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_service_invoices_filpride_customers_customer_id",
                table: "filpride_service_invoices",
                column: "customer_id",
                principalTable: "filpride_customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_service_invoices_filpride_services_service_id",
                table: "filpride_service_invoices",
                column: "service_id",
                principalTable: "filpride_services",
                principalColumn: "service_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
