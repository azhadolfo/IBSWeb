using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountsReceivableTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "filpride_bank_accounts",
                columns: table => new
                {
                    bank_account_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bank_account_no = table.Column<string>(type: "text", nullable: false),
                    bank = table.Column<string>(type: "text", nullable: false),
                    branch = table.Column<string>(type: "text", nullable: false),
                    bank_code = table.Column<string>(type: "text", nullable: true),
                    account_no = table.Column<string>(type: "text", nullable: false),
                    account_name = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    account_no_coa = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_bank_accounts", x => x.bank_account_id);
                });

            migrationBuilder.CreateTable(
                name: "filpride_sales_invoices",
                columns: table => new
                {
                    sales_invoice_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sales_invoice_no = table.Column<string>(type: "varchar(12)", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    other_ref_no = table.Column<string>(type: "varchar(20)", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    remarks = table.Column<string>(type: "varchar(100)", nullable: false),
                    vatable_sales = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", nullable: false),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    discount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    net_discount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    vat_exempt = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    zero_rated = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    with_holding_vat_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    with_holding_tax_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    is_tax_and_vat_paid = table.Column<bool>(type: "boolean", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    purchase_order_id = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_filpride_sales_invoices", x => x.sales_invoice_id);
                    table.ForeignKey(
                        name: "fk_filpride_sales_invoices_filpride_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "filpride_customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_filpride_sales_invoices_filpride_purchase_orders_purchase_o",
                        column: x => x.purchase_order_id,
                        principalTable: "filpride_purchase_orders",
                        principalColumn: "purchase_order_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_filpride_sales_invoices_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "filpride_services",
                columns: table => new
                {
                    service_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_no = table.Column<string>(type: "text", nullable: false),
                    current_and_previous_no = table.Column<string>(type: "varchar(20)", nullable: true),
                    current_and_previous_title = table.Column<string>(type: "varchar(50)", nullable: true),
                    unearned_title = table.Column<string>(type: "varchar(50)", nullable: true),
                    unearned_no = table.Column<string>(type: "varchar(20)", nullable: true),
                    name = table.Column<string>(type: "varchar(50)", nullable: false),
                    percent = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_services", x => x.service_id);
                });

            migrationBuilder.CreateTable(
                name: "filpride_service_invoices",
                columns: table => new
                {
                    service_invoice_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_invoice_no = table.Column<string>(type: "varchar(12)", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    service_id = table.Column<int>(type: "integer", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    period = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    net_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    discount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    withholding_tax_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    withholding_vat_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    current_and_previous_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unearned_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    instructions = table.Column<string>(type: "varchar(200)", nullable: true),
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
                    table.PrimaryKey("pk_filpride_service_invoices", x => x.service_invoice_id);
                    table.ForeignKey(
                        name: "fk_filpride_service_invoices_filpride_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "filpride_customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_filpride_service_invoices_filpride_services_service_id",
                        column: x => x.service_id,
                        principalTable: "filpride_services",
                        principalColumn: "service_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "filpride_collection_receipts",
                columns: table => new
                {
                    collection_receipt_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    collection_receipt_no = table.Column<string>(type: "varchar(12)", nullable: false),
                    sales_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    si_no = table.Column<string>(type: "varchar(12)", nullable: true),
                    multiple_si_id = table.Column<int[]>(type: "integer[]", nullable: true),
                    multiple_si = table.Column<string[]>(type: "text[]", nullable: true),
                    service_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    sv_no = table.Column<string>(type: "varchar(12)", nullable: true),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    series_number = table.Column<long>(type: "bigint", nullable: false),
                    reference_no = table.Column<string>(type: "varchar(20)", nullable: false),
                    remarks = table.Column<string>(type: "varchar(100)", nullable: true),
                    cash_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    check_date = table.Column<string>(type: "text", nullable: true),
                    check_no = table.Column<string>(type: "varchar(20)", nullable: true),
                    check_bank = table.Column<string>(type: "varchar(20)", nullable: true),
                    check_branch = table.Column<string>(type: "varchar(20)", nullable: true),
                    check_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    manager_check_date = table.Column<DateOnly>(type: "date", nullable: true),
                    manager_check_no = table.Column<string>(type: "varchar(20)", nullable: true),
                    manager_check_bank = table.Column<string>(type: "varchar(20)", nullable: true),
                    manager_check_branch = table.Column<string>(type: "varchar(20)", nullable: true),
                    manager_check_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ewt = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    wvat = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_certificate_upload = table.Column<bool>(type: "boolean", nullable: false),
                    f2306file_path = table.Column<string>(type: "varchar(200)", nullable: true),
                    f2307file_path = table.Column<string>(type: "varchar(200)", nullable: true),
                    si_multiple_amount = table.Column<decimal[]>(type: "numeric[]", nullable: true),
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
                    table.PrimaryKey("pk_filpride_collection_receipts", x => x.collection_receipt_id);
                    table.ForeignKey(
                        name: "fk_filpride_collection_receipts_filpride_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "filpride_customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_filpride_collection_receipts_filpride_sales_invoices_sales_",
                        column: x => x.sales_invoice_id,
                        principalTable: "filpride_sales_invoices",
                        principalColumn: "sales_invoice_id");
                    table.ForeignKey(
                        name: "fk_filpride_collection_receipts_filpride_service_invoices_serv",
                        column: x => x.service_invoice_id,
                        principalTable: "filpride_service_invoices",
                        principalColumn: "service_invoice_id");
                });

            migrationBuilder.CreateTable(
                name: "filpride_credit_memos",
                columns: table => new
                {
                    credit_memo_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    credit_memo_no = table.Column<string>(type: "varchar(12)", nullable: false),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    sales_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    service_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    description = table.Column<string>(type: "text", nullable: false),
                    adjusted_price = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    credit_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    vatable_sales = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_sales = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    with_holding_vat_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    with_holding_tax_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    remarks = table.Column<string>(type: "text", nullable: true),
                    period = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    current_and_previous_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unearned_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    services_id = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_filpride_credit_memos", x => x.credit_memo_id);
                    table.ForeignKey(
                        name: "fk_filpride_credit_memos_filpride_sales_invoices_sales_invoice",
                        column: x => x.sales_invoice_id,
                        principalTable: "filpride_sales_invoices",
                        principalColumn: "sales_invoice_id");
                    table.ForeignKey(
                        name: "fk_filpride_credit_memos_filpride_service_invoices_service_inv",
                        column: x => x.service_invoice_id,
                        principalTable: "filpride_service_invoices",
                        principalColumn: "service_invoice_id");
                });

            migrationBuilder.CreateTable(
                name: "filpride_debit_memos",
                columns: table => new
                {
                    debit_memo_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    debit_memo_no = table.Column<string>(type: "varchar(12)", nullable: false),
                    sales_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    service_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    dm_no = table.Column<string>(type: "text", nullable: true),
                    series_number = table.Column<long>(type: "bigint", nullable: false),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    debit_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    vatable_sales = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_sales = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    adjusted_price = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    with_holding_vat_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    with_holding_tax_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    remarks = table.Column<string>(type: "text", nullable: false),
                    period = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    current_and_previous_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    unearned_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    services_id = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_filpride_debit_memos", x => x.debit_memo_id);
                    table.ForeignKey(
                        name: "fk_filpride_debit_memos_filpride_sales_invoices_sales_invoice_",
                        column: x => x.sales_invoice_id,
                        principalTable: "filpride_sales_invoices",
                        principalColumn: "sales_invoice_id");
                    table.ForeignKey(
                        name: "fk_filpride_debit_memos_filpride_service_invoices_service_invo",
                        column: x => x.service_invoice_id,
                        principalTable: "filpride_service_invoices",
                        principalColumn: "service_invoice_id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_filpride_collection_receipts_customer_id",
                table: "filpride_collection_receipts",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_collection_receipts_sales_invoice_id",
                table: "filpride_collection_receipts",
                column: "sales_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_collection_receipts_service_invoice_id",
                table: "filpride_collection_receipts",
                column: "service_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_credit_memos_sales_invoice_id",
                table: "filpride_credit_memos",
                column: "sales_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_credit_memos_service_invoice_id",
                table: "filpride_credit_memos",
                column: "service_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_debit_memos_sales_invoice_id",
                table: "filpride_debit_memos",
                column: "sales_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_debit_memos_service_invoice_id",
                table: "filpride_debit_memos",
                column: "service_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_sales_invoices_customer_id",
                table: "filpride_sales_invoices",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_sales_invoices_product_id",
                table: "filpride_sales_invoices",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_sales_invoices_purchase_order_id",
                table: "filpride_sales_invoices",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_service_invoices_customer_id",
                table: "filpride_service_invoices",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_service_invoices_service_id",
                table: "filpride_service_invoices",
                column: "service_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "filpride_bank_accounts");

            migrationBuilder.DropTable(
                name: "filpride_collection_receipts");

            migrationBuilder.DropTable(
                name: "filpride_credit_memos");

            migrationBuilder.DropTable(
                name: "filpride_debit_memos");

            migrationBuilder.DropTable(
                name: "filpride_sales_invoices");

            migrationBuilder.DropTable(
                name: "filpride_service_invoices");

            migrationBuilder.DropTable(
                name: "filpride_services");
        }
    }
}
