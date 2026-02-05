using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTheIndexOfSeriesToPreventDuplicateSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_filpride_delivery_receipts_delivery_receipt_no",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropIndex(
                name: "ix_filpride_customer_order_slips_customer_order_slip_no",
                table: "filpride_customer_order_slips");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_service_invoices_service_invoice_no_company",
                table: "filpride_service_invoices",
                columns: new[] { "service_invoice_no", "company" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_sales_invoices_sales_invoice_no_company",
                table: "filpride_sales_invoices",
                columns: new[] { "sales_invoice_no", "company" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_receiving_reports_receiving_report_no_company",
                table: "filpride_receiving_reports",
                columns: new[] { "receiving_report_no", "company" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_purchase_orders_purchase_order_no_company",
                table: "filpride_purchase_orders",
                columns: new[] { "purchase_order_no", "company" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_journal_voucher_headers_journal_voucher_header_no_",
                table: "filpride_journal_voucher_headers",
                columns: new[] { "journal_voucher_header_no", "company" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_receipts_delivery_receipt_no_company",
                table: "filpride_delivery_receipts",
                columns: new[] { "delivery_receipt_no", "company" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_debit_memos_debit_memo_no_company",
                table: "filpride_debit_memos",
                columns: new[] { "debit_memo_no", "company" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_order_slips_customer_order_slip_no_company",
                table: "filpride_customer_order_slips",
                columns: new[] { "customer_order_slip_no", "company" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_credit_memos_credit_memo_no_company",
                table: "filpride_credit_memos",
                columns: new[] { "credit_memo_no", "company" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_collection_receipts_collection_receipt_no_company",
                table: "filpride_collection_receipts",
                columns: new[] { "collection_receipt_no", "company" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_check_voucher_headers_check_voucher_header_no_comp",
                table: "filpride_check_voucher_headers",
                columns: new[] { "check_voucher_header_no", "company" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_authority_to_loads_authority_to_load_no_company",
                table: "filpride_authority_to_loads",
                columns: new[] { "authority_to_load_no", "company" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_filpride_service_invoices_service_invoice_no_company",
                table: "filpride_service_invoices");

            migrationBuilder.DropIndex(
                name: "ix_filpride_sales_invoices_sales_invoice_no_company",
                table: "filpride_sales_invoices");

            migrationBuilder.DropIndex(
                name: "ix_filpride_receiving_reports_receiving_report_no_company",
                table: "filpride_receiving_reports");

            migrationBuilder.DropIndex(
                name: "ix_filpride_purchase_orders_purchase_order_no_company",
                table: "filpride_purchase_orders");

            migrationBuilder.DropIndex(
                name: "ix_filpride_journal_voucher_headers_journal_voucher_header_no_",
                table: "filpride_journal_voucher_headers");

            migrationBuilder.DropIndex(
                name: "ix_filpride_delivery_receipts_delivery_receipt_no_company",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropIndex(
                name: "ix_filpride_debit_memos_debit_memo_no_company",
                table: "filpride_debit_memos");

            migrationBuilder.DropIndex(
                name: "ix_filpride_customer_order_slips_customer_order_slip_no_company",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropIndex(
                name: "ix_filpride_credit_memos_credit_memo_no_company",
                table: "filpride_credit_memos");

            migrationBuilder.DropIndex(
                name: "ix_filpride_collection_receipts_collection_receipt_no_company",
                table: "filpride_collection_receipts");

            migrationBuilder.DropIndex(
                name: "ix_filpride_check_voucher_headers_check_voucher_header_no_comp",
                table: "filpride_check_voucher_headers");

            migrationBuilder.DropIndex(
                name: "ix_filpride_authority_to_loads_authority_to_load_no_company",
                table: "filpride_authority_to_loads");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_receipts_delivery_receipt_no",
                table: "filpride_delivery_receipts",
                column: "delivery_receipt_no");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_order_slips_customer_order_slip_no",
                table: "filpride_customer_order_slips",
                column: "customer_order_slip_no");
        }
    }
}
