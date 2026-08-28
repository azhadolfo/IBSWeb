using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCompanyFromFilprideTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                name: "ix_filpride_provisional_receipts_series_number_company",
                table: "filpride_provisional_receipts");

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

            migrationBuilder.DropColumn(
                name: "company",
                table: "posted_periods");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_service_invoices");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_sales_invoices");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_receiving_reports");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_purchase_orders");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_provisional_receipts");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_offsettings");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_journal_voucher_headers");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_inventories");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_debit_memos");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_credit_memos");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_collection_receipts");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_check_voucher_headers");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_audit_trails");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_service_invoices_service_invoice_no",
                table: "filpride_service_invoices",
                column: "service_invoice_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_sales_invoices_sales_invoice_no",
                table: "filpride_sales_invoices",
                column: "sales_invoice_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_receiving_reports_receiving_report_no",
                table: "filpride_receiving_reports",
                column: "receiving_report_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_purchase_orders_purchase_order_no",
                table: "filpride_purchase_orders",
                column: "purchase_order_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_provisional_receipts_series_number",
                table: "filpride_provisional_receipts",
                column: "series_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_journal_voucher_headers_journal_voucher_header_no",
                table: "filpride_journal_voucher_headers",
                column: "journal_voucher_header_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_receipts_delivery_receipt_no",
                table: "filpride_delivery_receipts",
                column: "delivery_receipt_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_debit_memos_debit_memo_no",
                table: "filpride_debit_memos",
                column: "debit_memo_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_order_slips_customer_order_slip_no",
                table: "filpride_customer_order_slips",
                column: "customer_order_slip_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_credit_memos_credit_memo_no",
                table: "filpride_credit_memos",
                column: "credit_memo_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_collection_receipts_collection_receipt_no",
                table: "filpride_collection_receipts",
                column: "collection_receipt_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_check_voucher_headers_check_voucher_header_no",
                table: "filpride_check_voucher_headers",
                column: "check_voucher_header_no",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_filpride_service_invoices_service_invoice_no",
                table: "filpride_service_invoices");

            migrationBuilder.DropIndex(
                name: "ix_filpride_sales_invoices_sales_invoice_no",
                table: "filpride_sales_invoices");

            migrationBuilder.DropIndex(
                name: "ix_filpride_receiving_reports_receiving_report_no",
                table: "filpride_receiving_reports");

            migrationBuilder.DropIndex(
                name: "ix_filpride_purchase_orders_purchase_order_no",
                table: "filpride_purchase_orders");

            migrationBuilder.DropIndex(
                name: "ix_filpride_provisional_receipts_series_number",
                table: "filpride_provisional_receipts");

            migrationBuilder.DropIndex(
                name: "ix_filpride_journal_voucher_headers_journal_voucher_header_no",
                table: "filpride_journal_voucher_headers");

            migrationBuilder.DropIndex(
                name: "ix_filpride_delivery_receipts_delivery_receipt_no",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropIndex(
                name: "ix_filpride_debit_memos_debit_memo_no",
                table: "filpride_debit_memos");

            migrationBuilder.DropIndex(
                name: "ix_filpride_customer_order_slips_customer_order_slip_no",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropIndex(
                name: "ix_filpride_credit_memos_credit_memo_no",
                table: "filpride_credit_memos");

            migrationBuilder.DropIndex(
                name: "ix_filpride_collection_receipts_collection_receipt_no",
                table: "filpride_collection_receipts");

            migrationBuilder.DropIndex(
                name: "ix_filpride_check_voucher_headers_check_voucher_header_no",
                table: "filpride_check_voucher_headers");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "posted_periods",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_service_invoices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_sales_invoices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_receiving_reports",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_purchase_orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_provisional_receipts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_offsettings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_journal_voucher_headers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_inventories",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_general_ledger_books",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_delivery_receipts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_debit_memos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_customer_order_slips",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_credit_memos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_collection_receipts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_check_voucher_headers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_audit_trails",
                type: "text",
                nullable: false,
                defaultValue: "");

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
                name: "ix_filpride_provisional_receipts_series_number_company",
                table: "filpride_provisional_receipts",
                columns: new[] { "series_number", "company" },
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
        }
    }
}
