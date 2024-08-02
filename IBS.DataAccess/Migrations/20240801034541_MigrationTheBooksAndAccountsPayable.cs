using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class MigrationTheBooksAndAccountsPayable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "service_invoice_no",
                table: "filpride_service_invoices",
                type: "varchar(12)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(12)");

            migrationBuilder.AlterColumn<string>(
                name: "sales_invoice_no",
                table: "filpride_sales_invoices",
                type: "varchar(12)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(12)");

            migrationBuilder.AlterColumn<string>(
                name: "debit_memo_no",
                table: "filpride_debit_memos",
                type: "varchar(12)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(12)");

            migrationBuilder.AlterColumn<string>(
                name: "credit_memo_no",
                table: "filpride_credit_memos",
                type: "varchar(12)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(12)");

            migrationBuilder.AlterColumn<string>(
                name: "collection_receipt_no",
                table: "filpride_collection_receipts",
                type: "varchar(12)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(12)");

            migrationBuilder.CreateTable(
                name: "filpride_cash_receipt_books",
                columns: table => new
                {
                    cash_receipt_book_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    ref_no = table.Column<string>(type: "text", nullable: false),
                    customer_name = table.Column<string>(type: "text", nullable: false),
                    bank = table.Column<string>(type: "text", nullable: true),
                    check_no = table.Column<string>(type: "text", nullable: true),
                    coa = table.Column<string>(type: "text", nullable: false),
                    particulars = table.Column<string>(type: "text", nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_cash_receipt_books", x => x.cash_receipt_book_id);
                });

            migrationBuilder.CreateTable(
                name: "filpride_check_voucher_details",
                columns: table => new
                {
                    check_voucher_detail_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_no = table.Column<string>(type: "text", nullable: false),
                    account_name = table.Column<string>(type: "text", nullable: false),
                    transaction_no = table.Column<string>(type: "text", nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_check_voucher_details", x => x.check_voucher_detail_id);
                });

            migrationBuilder.CreateTable(
                name: "filpride_check_voucher_headers",
                columns: table => new
                {
                    check_voucher_header_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    check_voucher_header_no = table.Column<string>(type: "text", nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    rr_no = table.Column<string[]>(type: "varchar[]", nullable: true),
                    si_no = table.Column<string[]>(type: "varchar[]", nullable: true),
                    po_no = table.Column<string[]>(type: "varchar[]", nullable: true),
                    supplier_id = table.Column<int>(type: "integer", nullable: true),
                    total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    amount = table.Column<decimal[]>(type: "numeric[]", nullable: true),
                    particulars = table.Column<string>(type: "text", nullable: true),
                    bank_id = table.Column<int>(type: "integer", nullable: true),
                    check_no = table.Column<string>(type: "text", nullable: true),
                    category = table.Column<string>(type: "text", nullable: false),
                    payee = table.Column<string>(type: "text", nullable: true),
                    check_date = table.Column<DateOnly>(type: "date", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    number_of_months = table.Column<int>(type: "integer", nullable: false),
                    number_of_months_created = table.Column<int>(type: "integer", nullable: false),
                    last_created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    amount_per_month = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    is_complete = table.Column<bool>(type: "boolean", nullable: false),
                    accrued_type = table.Column<string>(type: "text", nullable: true),
                    reference = table.Column<string>(type: "text", nullable: true),
                    cv_type = table.Column<string>(type: "varchar(10)", nullable: true),
                    check_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_check_voucher_headers", x => x.check_voucher_header_id);
                    table.ForeignKey(
                        name: "fk_filpride_check_voucher_headers_filpride_bank_accounts_bank_",
                        column: x => x.bank_id,
                        principalTable: "filpride_bank_accounts",
                        principalColumn: "bank_account_id");
                    table.ForeignKey(
                        name: "fk_filpride_check_voucher_headers_filpride_suppliers_supplier_",
                        column: x => x.supplier_id,
                        principalTable: "filpride_suppliers",
                        principalColumn: "supplier_id");
                });

            migrationBuilder.CreateTable(
                name: "filpride_disbursement_books",
                columns: table => new
                {
                    disbursement_book_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    cv_no = table.Column<string>(type: "text", nullable: false),
                    payee = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    particulars = table.Column<string>(type: "text", nullable: false),
                    bank = table.Column<string>(type: "text", nullable: false),
                    check_no = table.Column<string>(type: "text", nullable: false),
                    check_date = table.Column<string>(type: "text", nullable: false),
                    chart_of_account = table.Column<string>(type: "text", nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_disbursement_books", x => x.disbursement_book_id);
                });

            migrationBuilder.CreateTable(
                name: "filpride_general_ledger_books",
                columns: table => new
                {
                    general_ledger_book_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    reference = table.Column<string>(type: "text", nullable: false),
                    account_no = table.Column<string>(type: "text", nullable: false),
                    account_title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_posted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_general_ledger_books", x => x.general_ledger_book_id);
                });

            migrationBuilder.CreateTable(
                name: "filpride_journal_books",
                columns: table => new
                {
                    journal_book_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    reference = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    account_title = table.Column<string>(type: "text", nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_journal_books", x => x.journal_book_id);
                });

            migrationBuilder.CreateTable(
                name: "filpride_journal_voucher_details",
                columns: table => new
                {
                    journal_voucher_detail_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_no = table.Column<string>(type: "text", nullable: false),
                    account_name = table.Column<string>(type: "text", nullable: false),
                    transaction_no = table.Column<string>(type: "text", nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_journal_voucher_details", x => x.journal_voucher_detail_id);
                });

            migrationBuilder.CreateTable(
                name: "filpride_purchase_books",
                columns: table => new
                {
                    purchase_book_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    supplier_name = table.Column<string>(type: "text", nullable: false),
                    supplier_tin = table.Column<string>(type: "text", nullable: false),
                    supplier_address = table.Column<string>(type: "text", nullable: false),
                    document_no = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    discount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    wht_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    net_purchases = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    po_no = table.Column<string>(type: "varchar(12)", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_purchase_books", x => x.purchase_book_id);
                });

            migrationBuilder.CreateTable(
                name: "filpride_sales_books",
                columns: table => new
                {
                    sales_book_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    serial_no = table.Column<string>(type: "text", nullable: false),
                    sold_to = table.Column<string>(type: "text", nullable: false),
                    tin_no = table.Column<string>(type: "text", nullable: false),
                    address = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    vatable_sales = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    vat_exempt_sales = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    zero_rated = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    discount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    net_sales = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    document_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_sales_books", x => x.sales_book_id);
                });

            migrationBuilder.CreateTable(
                name: "purchase_orders",
                columns: table => new
                {
                    purchase_order_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    purchase_order_no = table.Column<string>(type: "text", nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    supplier_no = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    product_no = table.Column<string>(type: "text", nullable: true),
                    terms = table.Column<string>(type: "varchar(5)", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    final_price = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    quantity_received = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_received = table.Column<bool>(type: "boolean", nullable: false),
                    received_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    remarks = table.Column<string>(type: "varchar(200)", nullable: false),
                    is_closed = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_orders", x => x.purchase_order_id);
                    table.ForeignKey(
                        name: "fk_purchase_orders_filpride_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "filpride_suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_purchase_orders_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "filpride_journal_voucher_headers",
                columns: table => new
                {
                    journal_voucher_header_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    journal_voucher_header_no = table.Column<string>(type: "text", nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    references = table.Column<string>(type: "text", nullable: true),
                    cv_id = table.Column<int>(type: "integer", nullable: true),
                    particulars = table.Column<string>(type: "text", nullable: false),
                    cr_no = table.Column<string>(type: "text", nullable: true),
                    jv_reason = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_journal_voucher_headers", x => x.journal_voucher_header_id);
                    table.ForeignKey(
                        name: "fk_filpride_journal_voucher_headers_filpride_check_voucher_hea",
                        column: x => x.cv_id,
                        principalTable: "filpride_check_voucher_headers",
                        principalColumn: "check_voucher_header_id");
                });

            migrationBuilder.CreateTable(
                name: "receiving_reports",
                columns: table => new
                {
                    receiving_report_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    receiving_report_no = table.Column<string>(type: "text", nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    po_id = table.Column<int>(type: "integer", nullable: false),
                    po_no = table.Column<string>(type: "varchar(12)", nullable: true),
                    supplier_invoice_number = table.Column<string>(type: "varchar(100)", nullable: true),
                    supplier_invoice_date = table.Column<string>(type: "text", nullable: true),
                    truck_or_vessels = table.Column<string>(type: "varchar(100)", nullable: false),
                    quantity_delivered = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    quantity_received = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    gain_or_loss = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    net_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ewt_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    other_ref = table.Column<string>(type: "varchar(100)", nullable: true),
                    remarks = table.Column<string>(type: "text", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    paid_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    canceled_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    net_amount_of_ewt = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receiving_reports", x => x.receiving_report_id);
                    table.ForeignKey(
                        name: "fk_receiving_reports_purchase_orders_po_id",
                        column: x => x.po_id,
                        principalTable: "purchase_orders",
                        principalColumn: "purchase_order_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_filpride_check_voucher_headers_bank_id",
                table: "filpride_check_voucher_headers",
                column: "bank_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_check_voucher_headers_supplier_id",
                table: "filpride_check_voucher_headers",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_journal_voucher_headers_cv_id",
                table: "filpride_journal_voucher_headers",
                column: "cv_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_product_id",
                table: "purchase_orders",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_supplier_id",
                table: "purchase_orders",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_receiving_reports_po_id",
                table: "receiving_reports",
                column: "po_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "filpride_cash_receipt_books");

            migrationBuilder.DropTable(
                name: "filpride_check_voucher_details");

            migrationBuilder.DropTable(
                name: "filpride_disbursement_books");

            migrationBuilder.DropTable(
                name: "filpride_general_ledger_books");

            migrationBuilder.DropTable(
                name: "filpride_journal_books");

            migrationBuilder.DropTable(
                name: "filpride_journal_voucher_details");

            migrationBuilder.DropTable(
                name: "filpride_journal_voucher_headers");

            migrationBuilder.DropTable(
                name: "filpride_purchase_books");

            migrationBuilder.DropTable(
                name: "filpride_sales_books");

            migrationBuilder.DropTable(
                name: "receiving_reports");

            migrationBuilder.DropTable(
                name: "filpride_check_voucher_headers");

            migrationBuilder.DropTable(
                name: "purchase_orders");

            migrationBuilder.AlterColumn<string>(
                name: "service_invoice_no",
                table: "filpride_service_invoices",
                type: "varchar(12)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(12)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "sales_invoice_no",
                table: "filpride_sales_invoices",
                type: "varchar(12)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(12)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "debit_memo_no",
                table: "filpride_debit_memos",
                type: "varchar(12)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(12)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "credit_memo_no",
                table: "filpride_credit_memos",
                type: "varchar(12)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(12)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "collection_receipt_no",
                table: "filpride_collection_receipts",
                type: "varchar(12)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(12)",
                oldNullable: true);
        }
    }
}
