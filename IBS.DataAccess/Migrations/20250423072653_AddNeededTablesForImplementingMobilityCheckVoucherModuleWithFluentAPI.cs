using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddNeededTablesForImplementingMobilityCheckVoucherModuleWithFluentAPI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_check_voucher_details_filpride_check_voucher_heade",
                table: "filpride_check_voucher_details");

            migrationBuilder.CreateTable(
                name: "mobility_employees",
                columns: table => new
                {
                    employee_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_number = table.Column<string>(type: "varchar(10)", nullable: false),
                    initial = table.Column<string>(type: "text", nullable: true),
                    first_name = table.Column<string>(type: "varchar(100)", nullable: false),
                    middle_name = table.Column<string>(type: "varchar(100)", nullable: true),
                    last_name = table.Column<string>(type: "varchar(100)", nullable: false),
                    suffix = table.Column<string>(type: "varchar(5)", nullable: true),
                    address = table.Column<string>(type: "varchar(255)", nullable: true),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: true),
                    tel_no = table.Column<string>(type: "text", nullable: true),
                    sss_no = table.Column<string>(type: "text", nullable: true),
                    tin_no = table.Column<string>(type: "varchar(20)", nullable: true),
                    philhealth_no = table.Column<string>(type: "text", nullable: true),
                    pagibig_no = table.Column<string>(type: "text", nullable: true),
                    company = table.Column<string>(type: "text", nullable: true),
                    department = table.Column<string>(type: "text", nullable: true),
                    date_hired = table.Column<DateOnly>(type: "date", nullable: false),
                    date_resigned = table.Column<DateOnly>(type: "date", nullable: true),
                    position = table.Column<string>(type: "text", nullable: false),
                    is_managerial = table.Column<bool>(type: "boolean", nullable: false),
                    supervisor = table.Column<string>(type: "varchar(20)", nullable: false),
                    status = table.Column<string>(type: "varchar(5)", nullable: false),
                    paygrade = table.Column<string>(type: "text", nullable: true),
                    salary = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_employees", x => x.employee_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_check_voucher_headers",
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
                    total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
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
                    amount_per_month = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_complete = table.Column<bool>(type: "boolean", nullable: false),
                    accrued_type = table.Column<string>(type: "text", nullable: true),
                    reference = table.Column<string>(type: "text", nullable: true),
                    cv_type = table.Column<string>(type: "varchar(10)", nullable: true),
                    check_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    company = table.Column<string>(type: "text", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: true),
                    invoice_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    supporting_file_saved_file_name = table.Column<string>(type: "text", nullable: true),
                    supporting_file_saved_url = table.Column<string>(type: "text", nullable: true),
                    dcp_date = table.Column<DateOnly>(type: "date", nullable: true),
                    dcr_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_advances = table.Column<bool>(type: "boolean", nullable: false),
                    employee_id = table.Column<int>(type: "integer", nullable: true),
                    address = table.Column<string>(type: "text", nullable: false),
                    tin = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancellation_remarks = table.Column<string>(type: "varchar(255)", nullable: true),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_check_voucher_headers", x => x.check_voucher_header_id);
                    table.ForeignKey(
                        name: "fk_mobility_check_voucher_headers_mobility_bank_accounts_bank_",
                        column: x => x.bank_id,
                        principalTable: "mobility_bank_accounts",
                        principalColumn: "bank_account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mobility_check_voucher_headers_mobility_employees_employee_",
                        column: x => x.employee_id,
                        principalTable: "mobility_employees",
                        principalColumn: "employee_id");
                    table.ForeignKey(
                        name: "fk_mobility_check_voucher_headers_mobility_suppliers_supplier_",
                        column: x => x.supplier_id,
                        principalTable: "mobility_suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mobility_check_voucher_details",
                columns: table => new
                {
                    check_voucher_detail_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_no = table.Column<string>(type: "text", nullable: false),
                    account_name = table.Column<string>(type: "text", nullable: false),
                    transaction_no = table.Column<string>(type: "text", nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    check_voucher_header_id = table.Column<int>(type: "integer", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_vatable = table.Column<bool>(type: "boolean", nullable: false),
                    ewt_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    is_user_selected = table.Column<bool>(type: "boolean", nullable: false),
                    bank_id = table.Column<int>(type: "integer", nullable: true),
                    company_id = table.Column<int>(type: "integer", nullable: true),
                    customer_id = table.Column<int>(type: "integer", nullable: true),
                    employee_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_check_voucher_details", x => x.check_voucher_detail_id);
                    table.ForeignKey(
                        name: "fk_mobility_check_voucher_details_mobility_check_voucher_heade",
                        column: x => x.check_voucher_header_id,
                        principalTable: "mobility_check_voucher_headers",
                        principalColumn: "check_voucher_header_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mobility_check_voucher_details_mobility_suppliers_supplier_",
                        column: x => x.supplier_id,
                        principalTable: "mobility_suppliers",
                        principalColumn: "supplier_id");
                });

            migrationBuilder.CreateTable(
                name: "mobility_cv_trade_payments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    document_id = table.Column<int>(type: "integer", nullable: false),
                    document_type = table.Column<string>(type: "text", nullable: false),
                    check_voucher_id = table.Column<int>(type: "integer", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_cv_trade_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_mobility_cv_trade_payments_mobility_check_voucher_headers_c",
                        column: x => x.check_voucher_id,
                        principalTable: "mobility_check_voucher_headers",
                        principalColumn: "check_voucher_header_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mobility_multiple_check_voucher_payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    check_voucher_header_payment_id = table.Column<int>(type: "integer", nullable: false),
                    check_voucher_header_invoice_id = table.Column<int>(type: "integer", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_multiple_check_voucher_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_mobility_multiple_check_voucher_payments_mobility_check_vou",
                        column: x => x.check_voucher_header_invoice_id,
                        principalTable: "mobility_check_voucher_headers",
                        principalColumn: "check_voucher_header_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mobility_multiple_check_voucher_payments_mobility_check_vou1",
                        column: x => x.check_voucher_header_payment_id,
                        principalTable: "mobility_check_voucher_headers",
                        principalColumn: "check_voucher_header_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mobility_check_voucher_details_check_voucher_header_id",
                table: "mobility_check_voucher_details",
                column: "check_voucher_header_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_check_voucher_details_supplier_id",
                table: "mobility_check_voucher_details",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_check_voucher_headers_bank_id",
                table: "mobility_check_voucher_headers",
                column: "bank_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_check_voucher_headers_employee_id",
                table: "mobility_check_voucher_headers",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_check_voucher_headers_supplier_id",
                table: "mobility_check_voucher_headers",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_cv_trade_payments_check_voucher_id",
                table: "mobility_cv_trade_payments",
                column: "check_voucher_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_employees_employee_number",
                table: "mobility_employees",
                column: "employee_number");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_multiple_check_voucher_payments_check_voucher_head",
                table: "mobility_multiple_check_voucher_payments",
                column: "check_voucher_header_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_multiple_check_voucher_payments_check_voucher_head1",
                table: "mobility_multiple_check_voucher_payments",
                column: "check_voucher_header_payment_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_check_voucher_details_filpride_check_voucher_heade",
                table: "filpride_check_voucher_details",
                column: "check_voucher_header_id",
                principalTable: "filpride_check_voucher_headers",
                principalColumn: "check_voucher_header_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_check_voucher_details_filpride_check_voucher_heade",
                table: "filpride_check_voucher_details");

            migrationBuilder.DropTable(
                name: "mobility_check_voucher_details");

            migrationBuilder.DropTable(
                name: "mobility_cv_trade_payments");

            migrationBuilder.DropTable(
                name: "mobility_multiple_check_voucher_payments");

            migrationBuilder.DropTable(
                name: "mobility_check_voucher_headers");

            migrationBuilder.DropTable(
                name: "mobility_employees");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_check_voucher_details_filpride_check_voucher_heade",
                table: "filpride_check_voucher_details",
                column: "check_voucher_header_id",
                principalTable: "filpride_check_voucher_headers",
                principalColumn: "check_voucher_header_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
