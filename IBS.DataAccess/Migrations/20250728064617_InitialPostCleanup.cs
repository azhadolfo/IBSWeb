﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_settings",
                columns: table => new
                {
                    setting_key = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_settings", x => x.setting_key);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    department = table.Column<string>(type: "text", nullable: false),
                    station_access = table.Column<string>(type: "text", nullable: true),
                    position = table.Column<string>(type: "text", nullable: true),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "companies",
                columns: table => new
                {
                    company_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_code = table.Column<string>(type: "varchar(3)", nullable: true),
                    company_name = table.Column<string>(type: "varchar(50)", nullable: false),
                    company_address = table.Column<string>(type: "varchar(200)", nullable: false),
                    company_tin = table.Column<string>(type: "varchar(20)", nullable: false),
                    business_style = table.Column<string>(type: "varchar(20)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_companies", x => x.company_id);
                });

            migrationBuilder.CreateTable(
                name: "filpride_audit_trails",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    machine_name = table.Column<string>(type: "text", nullable: false),
                    activity = table.Column<string>(type: "text", nullable: false),
                    document_type = table.Column<string>(type: "text", nullable: false),
                    company = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_audit_trails", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "filpride_bank_accounts",
                columns: table => new
                {
                    bank_account_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bank = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    branch = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    account_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    account_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_filpride = table.Column<bool>(type: "boolean", nullable: false),
                    is_mobility = table.Column<bool>(type: "boolean", nullable: false),
                    is_bienes = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_bank_accounts", x => x.bank_account_id);
                });

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
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    company = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_cash_receipt_books", x => x.cash_receipt_book_id);
                });

            migrationBuilder.CreateTable(
                name: "filpride_chart_of_accounts",
                columns: table => new
                {
                    account_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    is_main = table.Column<bool>(type: "boolean", nullable: false),
                    account_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    account_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    account_type = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    normal_balance = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    level = table.Column<int>(type: "integer", nullable: false),
                    parent_account_id = table.Column<int>(type: "integer", nullable: true),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    has_children = table.Column<bool>(type: "boolean", nullable: false),
                    financial_statement_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_chart_of_accounts", x => x.account_id);
                    table.ForeignKey(
                        name: "fk_filpride_chart_of_accounts_filpride_chart_of_accounts_paren",
                        column: x => x.parent_account_id,
                        principalTable: "filpride_chart_of_accounts",
                        principalColumn: "account_id");
                });

            migrationBuilder.CreateTable(
                name: "filpride_customers",
                columns: table => new
                {
                    customer_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_code = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    customer_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    customer_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    customer_tin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    business_style = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    customer_terms = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    customer_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    vat_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    with_holding_vat = table.Column<bool>(type: "boolean", nullable: false),
                    with_holding_tax = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cluster_code = table.Column<int>(type: "integer", nullable: true),
                    station_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    credit_limit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit_limit_as_of_today = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    has_branch = table.Column<bool>(type: "boolean", nullable: false),
                    zip_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    retention_rate = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    has_multiple_terms = table.Column<bool>(type: "boolean", nullable: false),
                    is_filpride = table.Column<bool>(type: "boolean", nullable: false),
                    is_mobility = table.Column<bool>(type: "boolean", nullable: false),
                    is_bienes = table.Column<bool>(type: "boolean", nullable: false),
                    is_mmsi = table.Column<bool>(type: "boolean", nullable: false),
                    type = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    requires_price_adjustment = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_customers", x => x.customer_id);
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
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    company = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_disbursement_books", x => x.disbursement_book_id);
                });

            migrationBuilder.CreateTable(
                name: "filpride_employees",
                columns: table => new
                {
                    employee_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    initial = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    middle_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    suffix = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: true),
                    tel_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    sss_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    tin_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    philhealth_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    pagibig_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    department = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    date_hired = table.Column<DateOnly>(type: "date", nullable: false),
                    date_resigned = table.Column<DateOnly>(type: "date", nullable: true),
                    position = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_managerial = table.Column<bool>(type: "boolean", nullable: false),
                    supervisor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    paygrade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    salary = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_employees", x => x.employee_id);
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
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    company = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_journal_books", x => x.journal_book_id);
                });

            migrationBuilder.CreateTable(
                name: "filpride_monthly_nibits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    beginning_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    net_income = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    prior_period_adjustment = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ending_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    company = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_monthly_nibits", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "filpride_offsettings",
                columns: table => new
                {
                    off_setting_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_no = table.Column<string>(type: "text", nullable: false),
                    account_title = table.Column<string>(type: "text", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    reference = table.Column<string>(type: "text", nullable: true),
                    is_removed = table.Column<bool>(type: "boolean", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    company = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_offsettings", x => x.off_setting_id);
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
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    company = table.Column<string>(type: "text", nullable: false)
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
                    document_id = table.Column<int>(type: "integer", nullable: true),
                    company = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_sales_books", x => x.sales_book_id);
                });

            migrationBuilder.CreateTable(
                name: "filpride_services",
                columns: table => new
                {
                    service_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_no = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    current_and_previous_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    current_and_previous_title = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    unearned_title = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    unearned_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    percent = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_filpride = table.Column<bool>(type: "boolean", nullable: false),
                    is_mobility = table.Column<bool>(type: "boolean", nullable: false),
                    is_bienes = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_services", x => x.service_id);
                });

            migrationBuilder.CreateTable(
                name: "filpride_suppliers",
                columns: table => new
                {
                    supplier_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    supplier_code = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    supplier_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    supplier_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    supplier_tin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    supplier_terms = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    vat_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    tax_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    proof_of_registration_file_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    proof_of_registration_file_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    proof_of_exemption_file_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    proof_of_exemption_file_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    trade_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    branch = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    default_expense_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    withholding_tax_percent = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    withholding_taxtitle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    reason_of_exemption = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    validity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    validity_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    zip_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    is_filpride = table.Column<bool>(type: "boolean", nullable: false),
                    is_mobility = table.Column<bool>(type: "boolean", nullable: false),
                    is_bienes = table.Column<bool>(type: "boolean", nullable: false),
                    requires_price_adjustment = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_suppliers", x => x.supplier_id);
                });

            migrationBuilder.CreateTable(
                name: "hub_connections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    connection_id = table.Column<string>(type: "text", nullable: false),
                    user_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hub_connections", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "log_messages",
                columns: table => new
                {
                    log_id = table.Column<Guid>(type: "uuid", nullable: false),
                    time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    log_level = table.Column<string>(type: "text", nullable: false),
                    logger_name = table.Column<string>(type: "text", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_log_messages", x => x.log_id);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_ports",
                columns: table => new
                {
                    port_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    port_number = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: true),
                    port_name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    has_sbma = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_ports", x => x.port_id);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_services",
                columns: table => new
                {
                    service_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_number = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false),
                    service_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_services", x => x.service_id);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_tug_masters",
                columns: table => new
                {
                    tug_master_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tug_master_number = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false),
                    tug_master_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_tug_masters", x => x.tug_master_id);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_tugboat_owners",
                columns: table => new
                {
                    tugboat_owner_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tugboat_owner_number = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false),
                    tugboat_owner_name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    fixed_rate = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_tugboat_owners", x => x.tugboat_owner_id);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_user_accesses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    user_name = table.Column<string>(type: "varchar(100)", nullable: true),
                    can_create_service_request = table.Column<bool>(type: "boolean", nullable: false),
                    can_post_service_request = table.Column<bool>(type: "boolean", nullable: false),
                    can_create_dispatch_ticket = table.Column<bool>(type: "boolean", nullable: false),
                    can_set_tariff = table.Column<bool>(type: "boolean", nullable: false),
                    can_approve_tariff = table.Column<bool>(type: "boolean", nullable: false),
                    can_create_billing = table.Column<bool>(type: "boolean", nullable: false),
                    can_create_collection = table.Column<bool>(type: "boolean", nullable: false),
                    can_print_report = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_user_accesses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_vessels",
                columns: table => new
                {
                    vessel_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    vessel_number = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: false),
                    vessel_name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    vessel_type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_vessels", x => x.vessel_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_bank_accounts",
                columns: table => new
                {
                    bank_account_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bank = table.Column<string>(type: "text", nullable: false),
                    branch = table.Column<string>(type: "text", nullable: false),
                    account_no = table.Column<string>(type: "text", nullable: false),
                    account_name = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    station_code = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_bank_accounts", x => x.bank_account_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_chart_of_accounts",
                columns: table => new
                {
                    account_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    is_main = table.Column<bool>(type: "boolean", nullable: false),
                    account_number = table.Column<string>(type: "varchar(15)", nullable: true),
                    account_name = table.Column<string>(type: "varchar(100)", nullable: false),
                    account_type = table.Column<string>(type: "varchar(25)", nullable: true),
                    normal_balance = table.Column<string>(type: "varchar(20)", nullable: true),
                    level = table.Column<int>(type: "integer", nullable: false),
                    parent = table.Column<string>(type: "varchar(15)", nullable: true),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_chart_of_accounts", x => x.account_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_customers",
                columns: table => new
                {
                    customer_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    customer_code_name = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    station_code = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false),
                    customer_address = table.Column<string>(type: "varchar(200)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    quantity_limit = table.Column<decimal>(type: "numeric", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    customer_terms = table.Column<string>(type: "varchar(10)", nullable: false),
                    customer_tin = table.Column<string>(type: "text", nullable: false),
                    customer_type = table.Column<string>(type: "varchar(10)", nullable: false),
                    is_check_details_required = table.Column<bool>(type: "boolean", nullable: false),
                    customer_code = table.Column<string>(type: "varchar(7)", nullable: true),
                    business_style = table.Column<string>(type: "varchar(100)", nullable: true),
                    vat_type = table.Column<string>(type: "varchar(10)", nullable: false),
                    with_holding_vat = table.Column<bool>(type: "boolean", nullable: false),
                    with_holding_tax = table.Column<bool>(type: "boolean", nullable: false),
                    cluster_code = table.Column<int>(type: "integer", nullable: true),
                    credit_limit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit_limit_as_of_today = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    zip_code = table.Column<string>(type: "varchar(10)", nullable: false),
                    retention_rate = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    has_multiple_terms = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_customers", x => x.customer_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_fms_calibrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_code = table.Column<string>(type: "text", nullable: false),
                    shift_record_id = table.Column<string>(type: "text", nullable: false),
                    pump_number = table.Column<int>(type: "integer", nullable: false),
                    product_code = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    shift_date = table.Column<DateOnly>(type: "date", nullable: false),
                    shift_number = table.Column<int>(type: "integer", nullable: false),
                    page_number = table.Column<int>(type: "integer", nullable: false),
                    is_processed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_fms_calibrations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_fms_cashier_shifts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_code = table.Column<string>(type: "text", nullable: false),
                    shift_record_id = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    employee_number = table.Column<string>(type: "text", nullable: false),
                    shift_number = table.Column<int>(type: "integer", nullable: false),
                    page_number = table.Column<int>(type: "integer", nullable: false),
                    time_in = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    time_out = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    next_day = table.Column<bool>(type: "boolean", nullable: false),
                    cash_on_hand = table.Column<decimal>(type: "numeric", nullable: false),
                    biodiesel_price = table.Column<decimal>(type: "numeric", nullable: false),
                    econogas_price = table.Column<decimal>(type: "numeric", nullable: false),
                    envirogas_price = table.Column<decimal>(type: "numeric", nullable: false),
                    is_processed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_fms_cashier_shifts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_fms_deposits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_code = table.Column<string>(type: "text", nullable: false),
                    shift_record_id = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    account_number = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    shift_date = table.Column<DateOnly>(type: "date", nullable: false),
                    shift_number = table.Column<int>(type: "integer", nullable: false),
                    page_number = table.Column<int>(type: "integer", nullable: false),
                    approved_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    approved_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_fms_deposits", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_fms_fuel_sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_code = table.Column<string>(type: "text", nullable: false),
                    shift_record_id = table.Column<string>(type: "text", nullable: false),
                    pump_number = table.Column<int>(type: "integer", nullable: false),
                    product_code = table.Column<string>(type: "text", nullable: false),
                    opening = table.Column<decimal>(type: "numeric", nullable: false),
                    closing = table.Column<decimal>(type: "numeric", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    shift_date = table.Column<DateOnly>(type: "date", nullable: false),
                    shift_number = table.Column<int>(type: "integer", nullable: false),
                    page_number = table.Column<int>(type: "integer", nullable: false),
                    is_processed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_fms_fuel_sales", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_fms_lube_sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_code = table.Column<string>(type: "text", nullable: false),
                    shift_record_id = table.Column<string>(type: "text", nullable: false),
                    product_code = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    actual_price = table.Column<decimal>(type: "numeric", nullable: false),
                    cost = table.Column<decimal>(type: "numeric", nullable: false),
                    shift_date = table.Column<DateOnly>(type: "date", nullable: false),
                    shift_number = table.Column<int>(type: "integer", nullable: false),
                    page_number = table.Column<int>(type: "integer", nullable: false),
                    is_processed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_fms_lube_sales", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_fms_po_sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_code = table.Column<string>(type: "text", nullable: false),
                    shift_record_id = table.Column<string>(type: "text", nullable: false),
                    product_code = table.Column<string>(type: "text", nullable: false),
                    customer_code = table.Column<string>(type: "text", nullable: false),
                    trip_ticket = table.Column<string>(type: "text", nullable: false),
                    dr_number = table.Column<string>(type: "text", nullable: false),
                    driver = table.Column<string>(type: "text", nullable: false),
                    plate_no = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    contract_price = table.Column<decimal>(type: "numeric", nullable: false),
                    time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    shift_date = table.Column<DateOnly>(type: "date", nullable: false),
                    shift_number = table.Column<int>(type: "integer", nullable: false),
                    page_number = table.Column<int>(type: "integer", nullable: false),
                    is_processed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_fms_po_sales", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_fuel_deliveries",
                columns: table => new
                {
                    fuel_delivery_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pagenumber = table.Column<int>(type: "integer", nullable: false),
                    stncode = table.Column<string>(type: "varchar(5)", nullable: false),
                    cashiercode = table.Column<string>(type: "text", nullable: false),
                    shiftnumber = table.Column<int>(type: "integer", nullable: false),
                    shiftdate = table.Column<DateOnly>(type: "date", nullable: false),
                    timein = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    timeout = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    driver = table.Column<string>(type: "varchar(100)", nullable: false),
                    hauler = table.Column<string>(type: "varchar(100)", nullable: false),
                    platenumber = table.Column<string>(type: "varchar(50)", nullable: false),
                    drnumber = table.Column<string>(type: "varchar(50)", nullable: false),
                    wcnumber = table.Column<string>(type: "varchar(50)", nullable: false),
                    tanknumber = table.Column<int>(type: "integer", nullable: false),
                    productcode = table.Column<string>(type: "varchar(10)", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    purchaseprice = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    sellprice = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    volumebefore = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    volumeafter = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    receivedby = table.Column<string>(type: "varchar(50)", nullable: false),
                    createdby = table.Column<string>(type: "varchar(50)", nullable: false),
                    createddate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_fuel_deliveries", x => x.fuel_delivery_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_fuel_purchase",
                columns: table => new
                {
                    fuel_purchase_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fuel_purchase_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    page_number = table.Column<int>(type: "integer", nullable: false),
                    station_code = table.Column<string>(type: "varchar(5)", nullable: false),
                    cashier_code = table.Column<string>(type: "varchar(5)", nullable: false),
                    shift_no = table.Column<int>(type: "integer", nullable: false),
                    shift_date = table.Column<DateOnly>(type: "date", nullable: false),
                    time_in = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    time_out = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    driver = table.Column<string>(type: "varchar(100)", nullable: false),
                    hauler = table.Column<string>(type: "varchar(100)", nullable: false),
                    plate_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    dr_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    wc_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    tank_no = table.Column<int>(type: "integer", nullable: false),
                    product_code = table.Column<string>(type: "varchar(10)", nullable: false),
                    purchase_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    selling_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    quantity_before = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    quantity_after = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    should_be = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    gain_or_loss = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    received_by = table.Column<string>(type: "varchar(50)", nullable: false),
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
                    table.PrimaryKey("pk_mobility_fuel_purchase", x => x.fuel_purchase_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_fuels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    start = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    end = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    inv_date = table.Column<DateOnly>(type: "date", nullable: false),
                    x_corpcode = table.Column<int>(type: "integer", nullable: true),
                    x_sitecode = table.Column<int>(type: "integer", nullable: false),
                    x_tank = table.Column<int>(type: "integer", nullable: true),
                    x_pump = table.Column<int>(type: "integer", nullable: false),
                    x_nozzle = table.Column<int>(type: "integer", nullable: true),
                    x_year = table.Column<int>(type: "integer", nullable: true),
                    x_month = table.Column<int>(type: "integer", nullable: true),
                    x_day = table.Column<int>(type: "integer", nullable: true),
                    x_transaction = table.Column<int>(type: "integer", nullable: true),
                    price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount_db = table.Column<decimal>(type: "numeric", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    calibration = table.Column<decimal>(type: "numeric", nullable: false),
                    volume = table.Column<decimal>(type: "numeric", nullable: false),
                    item_code = table.Column<string>(type: "varchar(16)", nullable: false),
                    particulars = table.Column<string>(type: "varchar(32)", nullable: false),
                    opening = table.Column<decimal>(type: "numeric", nullable: true),
                    closing = table.Column<decimal>(type: "numeric", nullable: true),
                    nozdown = table.Column<string>(type: "varchar(20)", nullable: false),
                    in_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    out_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    liters = table.Column<decimal>(type: "numeric", nullable: true),
                    x_oid = table.Column<string>(type: "varchar(20)", nullable: true),
                    x_oname = table.Column<string>(type: "varchar(20)", nullable: false),
                    shift = table.Column<int>(type: "integer", nullable: false),
                    plateno = table.Column<string>(type: "varchar(20)", nullable: true),
                    pono = table.Column<string>(type: "varchar(20)", nullable: true),
                    cust = table.Column<string>(type: "varchar(20)", nullable: true),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    detail_group = table.Column<int>(type: "integer", nullable: false),
                    trans_count = table.Column<int>(type: "integer", nullable: false),
                    is_processed = table.Column<bool>(type: "boolean", nullable: false),
                    x_ticket_id = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_mobility_fuels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_general_ledgers",
                columns: table => new
                {
                    general_ledger_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reference = table.Column<string>(type: "varchar(100)", nullable: false),
                    particular = table.Column<string>(type: "varchar(200)", nullable: false),
                    account_number = table.Column<string>(type: "varchar(15)", nullable: false),
                    account_title = table.Column<string>(type: "varchar(200)", nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    station_code = table.Column<string>(type: "varchar(5)", nullable: false),
                    product_code = table.Column<string>(type: "varchar(20)", nullable: true),
                    supplier_code = table.Column<string>(type: "varchar(200)", nullable: true),
                    customer_code = table.Column<string>(type: "varchar(200)", nullable: true),
                    journal_reference = table.Column<string>(type: "varchar(50)", nullable: false),
                    is_validated = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_general_ledgers", x => x.general_ledger_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_inventories",
                columns: table => new
                {
                    inventory_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    particulars = table.Column<string>(type: "varchar(50)", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    reference = table.Column<string>(type: "varchar(200)", nullable: false),
                    product_code = table.Column<string>(type: "varchar(10)", nullable: false),
                    station_code = table.Column<string>(type: "varchar(10)", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    running_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    inventory_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit_cost_average = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    inventory_value = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    cost_of_goods_sold = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    validated_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    validated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    transaction_no = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_inventories", x => x.inventory_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_log_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference = table.Column<string>(type: "text", nullable: false),
                    reference_id = table.Column<int>(type: "integer", nullable: false),
                    module = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    original_value = table.Column<string>(type: "text", nullable: true),
                    adjusted_value = table.Column<string>(type: "text", nullable: true),
                    time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_log_reports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_lube_deliveries",
                columns: table => new
                {
                    lube_delivery_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pagenumber = table.Column<int>(type: "integer", nullable: false),
                    stncode = table.Column<string>(type: "varchar(5)", nullable: false),
                    cashiercode = table.Column<string>(type: "text", nullable: false),
                    shiftnumber = table.Column<int>(type: "integer", nullable: false),
                    shiftdate = table.Column<DateOnly>(type: "date", nullable: false),
                    suppliercode = table.Column<string>(type: "varchar(10)", nullable: false),
                    invoiceno = table.Column<string>(type: "varchar(50)", nullable: false),
                    drno = table.Column<string>(type: "varchar(50)", nullable: false),
                    pono = table.Column<string>(type: "varchar(50)", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    freight = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    rcvdby = table.Column<string>(type: "varchar(50)", nullable: false),
                    createdby = table.Column<string>(type: "varchar(50)", nullable: false),
                    createddate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit = table.Column<string>(type: "varchar(10)", nullable: false),
                    description = table.Column<string>(type: "varchar(200)", nullable: false),
                    unitprice = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    productcode = table.Column<string>(type: "varchar(10)", nullable: false),
                    piece = table.Column<int>(type: "integer", nullable: false),
                    srp = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_lube_deliveries", x => x.lube_delivery_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_lube_purchase_headers",
                columns: table => new
                {
                    lube_purchase_header_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lube_purchase_header_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    page_number = table.Column<int>(type: "integer", nullable: false),
                    station_code = table.Column<string>(type: "varchar(5)", nullable: false),
                    cashier_code = table.Column<string>(type: "varchar(5)", nullable: false),
                    shift_no = table.Column<int>(type: "integer", nullable: false),
                    shift_date = table.Column<DateOnly>(type: "date", nullable: false),
                    sales_invoice = table.Column<string>(type: "varchar(50)", nullable: false),
                    supplier_code = table.Column<string>(type: "varchar(10)", nullable: false),
                    dr_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    po_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    vatable_sales = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    received_by = table.Column<string>(type: "varchar(50)", nullable: false),
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
                    table.PrimaryKey("pk_mobility_lube_purchase_headers", x => x.lube_purchase_header_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_lubes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inv_date = table.Column<DateOnly>(type: "date", nullable: false),
                    x_year = table.Column<int>(type: "integer", nullable: true),
                    x_month = table.Column<int>(type: "integer", nullable: true),
                    x_day = table.Column<int>(type: "integer", nullable: true),
                    x_corpcode = table.Column<int>(type: "integer", nullable: true),
                    x_sitecode = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount_db = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    lubes_qty = table.Column<decimal>(type: "numeric", nullable: false),
                    item_code = table.Column<string>(type: "varchar(16)", nullable: false),
                    particulars = table.Column<string>(type: "varchar(100)", nullable: false),
                    x_oid = table.Column<string>(type: "varchar(10)", nullable: true),
                    cashier = table.Column<string>(type: "varchar(20)", nullable: false),
                    shift = table.Column<int>(type: "integer", nullable: false),
                    x_transaction = table.Column<int>(type: "integer", nullable: true),
                    x_stamp = table.Column<string>(type: "varchar(50)", nullable: false),
                    plateno = table.Column<string>(type: "varchar(20)", nullable: true),
                    pono = table.Column<string>(type: "varchar(20)", nullable: true),
                    cust = table.Column<string>(type: "varchar(20)", nullable: true),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_processed = table.Column<bool>(type: "boolean", nullable: false),
                    x_ticket_id = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_mobility_lubes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_offlines",
                columns: table => new
                {
                    offline_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    series_no = table.Column<int>(type: "integer", nullable: false),
                    station_code = table.Column<string>(type: "varchar(3)", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    product = table.Column<string>(type: "varchar(20)", nullable: false),
                    pump = table.Column<int>(type: "integer", nullable: false),
                    first_dsr_opening = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    first_dsr_closing = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    second_dsr_opening = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    second_dsr_closing = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    liters = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    first_dsr_no = table.Column<string>(type: "text", nullable: false),
                    second_dsr_no = table.Column<string>(type: "text", nullable: false),
                    is_resolve = table.Column<bool>(type: "boolean", nullable: false),
                    new_closing = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    last_updated_by = table.Column<string>(type: "text", nullable: true),
                    last_updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_offlines", x => x.offline_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_offsettings",
                columns: table => new
                {
                    off_setting_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_no = table.Column<string>(type: "text", nullable: false),
                    account_title = table.Column<string>(type: "text", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    reference = table.Column<string>(type: "text", nullable: true),
                    is_removed = table.Column<bool>(type: "boolean", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    station_code = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_offsettings", x => x.off_setting_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_po_sales",
                columns: table => new
                {
                    po_sales_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    po_sales_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    shift_rec_id = table.Column<string>(type: "varchar(20)", nullable: false),
                    station_code = table.Column<string>(type: "varchar(5)", nullable: false),
                    cashier_code = table.Column<string>(type: "varchar(5)", nullable: false),
                    shift_no = table.Column<int>(type: "integer", nullable: false),
                    po_sales_date = table.Column<DateOnly>(type: "date", nullable: false),
                    po_sales_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    customer_code = table.Column<string>(type: "varchar(20)", nullable: false),
                    driver = table.Column<string>(type: "varchar(50)", nullable: false),
                    plate_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    dr_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    trip_ticket = table.Column<string>(type: "varchar(20)", nullable: false),
                    product_code = table.Column<string>(type: "varchar(10)", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    contract_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
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
                    table.PrimaryKey("pk_mobility_po_sales", x => x.po_sales_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_po_sales_raw",
                columns: table => new
                {
                    po_sales_raw_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shiftrecid = table.Column<string>(type: "varchar(20)", nullable: false),
                    stncode = table.Column<string>(type: "varchar(5)", nullable: false),
                    cashiercode = table.Column<string>(type: "text", nullable: false),
                    shiftnumber = table.Column<int>(type: "integer", nullable: false),
                    podate = table.Column<DateOnly>(type: "date", nullable: false),
                    potime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    customercode = table.Column<string>(type: "varchar(20)", nullable: false),
                    driver = table.Column<string>(type: "varchar(50)", nullable: false),
                    plateno = table.Column<string>(type: "varchar(50)", nullable: false),
                    drnumber = table.Column<string>(type: "varchar(50)", nullable: false),
                    tripticket = table.Column<string>(type: "text", nullable: false),
                    productcode = table.Column<string>(type: "varchar(10)", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    contractprice = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    createdby = table.Column<string>(type: "varchar(50)", nullable: false),
                    createddate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_po_sales_raw", x => x.po_sales_raw_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_products",
                columns: table => new
                {
                    product_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_code = table.Column<string>(type: "varchar(10)", nullable: false),
                    product_name = table.Column<string>(type: "varchar(50)", nullable: false),
                    product_unit = table.Column<string>(type: "varchar(2)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_products", x => x.product_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_safe_drops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inv_date = table.Column<DateOnly>(type: "date", nullable: false),
                    b_date = table.Column<DateOnly>(type: "date", nullable: true),
                    x_year = table.Column<int>(type: "integer", nullable: true),
                    x_month = table.Column<int>(type: "integer", nullable: true),
                    x_day = table.Column<int>(type: "integer", nullable: true),
                    x_corpcode = table.Column<int>(type: "integer", nullable: true),
                    x_sitecode = table.Column<int>(type: "integer", nullable: true),
                    t_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    x_stamp = table.Column<string>(type: "varchar(50)", nullable: false),
                    x_oid = table.Column<string>(type: "varchar(10)", nullable: true),
                    x_oname = table.Column<string>(type: "varchar(20)", nullable: false),
                    shift = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_processed = table.Column<bool>(type: "boolean", nullable: false),
                    x_ticket_id = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_mobility_safe_drops", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_sales_headers",
                columns: table => new
                {
                    sales_header_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sales_no = table.Column<string>(type: "varchar(15)", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    cashier = table.Column<string>(type: "varchar(20)", nullable: false),
                    shift = table.Column<int>(type: "integer", nullable: false),
                    time_in = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    time_out = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    fuel_sales_total_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    lubes_total_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    safe_drop_total_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    po_sales_amount = table.Column<decimal[]>(type: "numeric(18,4)[]", nullable: false),
                    customers = table.Column<string[]>(type: "varchar[]", nullable: false),
                    po_sales_total_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_sales = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    gain_or_loss = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_transaction_normal = table.Column<bool>(type: "boolean", nullable: false),
                    station_code = table.Column<string>(type: "varchar(3)", nullable: false),
                    source = table.Column<string>(type: "varchar(10)", nullable: false),
                    actual_cash_on_hand = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    particular = table.Column<string>(type: "varchar(200)", nullable: true),
                    is_modified = table.Column<bool>(type: "boolean", nullable: false),
                    page_number = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_mobility_sales_headers", x => x.sales_header_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_services",
                columns: table => new
                {
                    service_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_no = table.Column<string>(type: "text", nullable: true),
                    current_and_previous_no = table.Column<string>(type: "varchar(20)", nullable: true),
                    current_and_previous_title = table.Column<string>(type: "varchar(50)", nullable: true),
                    unearned_title = table.Column<string>(type: "varchar(50)", nullable: true),
                    unearned_no = table.Column<string>(type: "varchar(20)", nullable: true),
                    name = table.Column<string>(type: "varchar(50)", nullable: false),
                    percent = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    station_code = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_services", x => x.service_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_station_employees",
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
                    station_code = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_mobility_station_employees", x => x.employee_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_station_pumps",
                columns: table => new
                {
                    pump_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    station_code = table.Column<string>(type: "varchar(15)", nullable: false),
                    product_code = table.Column<string>(type: "varchar(15)", nullable: false),
                    pos_pump = table.Column<int>(type: "integer", nullable: false),
                    fms_pump = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_station_pumps", x => x.pump_id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_stations",
                columns: table => new
                {
                    station_code = table.Column<string>(type: "varchar(5)", nullable: false),
                    station_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pos_code = table.Column<string>(type: "text", nullable: false),
                    station_name = table.Column<string>(type: "varchar(50)", nullable: false),
                    initial = table.Column<string>(type: "varchar(5)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    folder_path = table.Column<string>(type: "varchar(255)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_stations", x => x.station_code);
                });

            migrationBuilder.CreateTable(
                name: "mobility_suppliers",
                columns: table => new
                {
                    supplier_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    supplier_code = table.Column<string>(type: "varchar(7)", nullable: true),
                    supplier_name = table.Column<string>(type: "varchar(50)", nullable: false),
                    supplier_address = table.Column<string>(type: "varchar(200)", nullable: false),
                    supplier_tin = table.Column<string>(type: "varchar(20)", nullable: false),
                    supplier_terms = table.Column<string>(type: "varchar(10)", nullable: false),
                    vat_type = table.Column<string>(type: "varchar(10)", nullable: false),
                    tax_type = table.Column<string>(type: "varchar(20)", nullable: false),
                    proof_of_registration_file_path = table.Column<string>(type: "varchar(1024)", nullable: true),
                    proof_of_registration_file_name = table.Column<string>(type: "text", nullable: true),
                    proof_of_exemption_file_path = table.Column<string>(type: "varchar(1024)", nullable: true),
                    proof_of_exemption_file_name = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    category = table.Column<string>(type: "varchar(20)", nullable: false),
                    trade_name = table.Column<string>(type: "varchar(255)", nullable: true),
                    branch = table.Column<string>(type: "varchar(20)", nullable: true),
                    default_expense_number = table.Column<string>(type: "varchar(100)", nullable: true),
                    withholding_tax_percent = table.Column<decimal>(type: "numeric", nullable: true),
                    withholding_taxtitle = table.Column<string>(type: "varchar(100)", nullable: true),
                    reason_of_exemption = table.Column<string>(type: "varchar(100)", nullable: true),
                    validity = table.Column<string>(type: "varchar(20)", nullable: true),
                    validity_date = table.Column<DateTime>(type: "date", nullable: true),
                    station_code = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false),
                    zip_code = table.Column<string>(type: "varchar(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_suppliers", x => x.supplier_id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.notification_id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    product_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_code = table.Column<string>(type: "varchar(10)", nullable: false),
                    product_name = table.Column<string>(type: "varchar(50)", nullable: false),
                    product_unit = table.Column<string>(type: "varchar(2)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.product_id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<string>(type: "text", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_asp_net_role_claims_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "AspNetRoles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_asp_net_user_claims_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    provider_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_asp_net_user_logins_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    role_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_asp_net_user_roles_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "AspNetRoles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_asp_net_user_roles_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    login_provider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_asp_net_user_tokens_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bienes_placements",
                columns: table => new
                {
                    placement_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    control_number = table.Column<string>(type: "varchar(20)", nullable: false),
                    company_id = table.Column<int>(type: "integer", nullable: false),
                    bank_id = table.Column<int>(type: "integer", nullable: false),
                    bank = table.Column<string>(type: "varchar(20)", nullable: false),
                    branch = table.Column<string>(type: "varchar(100)", nullable: false),
                    account_name = table.Column<string>(type: "varchar(100)", nullable: false),
                    @class = table.Column<string>(name: "class", type: "varchar(10)", nullable: false),
                    settlement_account_id = table.Column<int>(type: "integer", nullable: false),
                    date_from = table.Column<DateOnly>(type: "date", nullable: false),
                    date_to = table.Column<DateOnly>(type: "date", nullable: false),
                    remarks = table.Column<string>(type: "varchar(255)", nullable: false),
                    cheque_number = table.Column<string>(type: "varchar(100)", nullable: false),
                    cv_no = table.Column<string>(type: "varchar(100)", nullable: false),
                    disposition = table.Column<string>(type: "varchar(5)", nullable: false),
                    principal_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    principal_disposition = table.Column<string>(type: "varchar(100)", nullable: true),
                    placement_type = table.Column<int>(type: "integer", nullable: false),
                    number_of_years = table.Column<int>(type: "integer", nullable: false),
                    interest_rate = table.Column<decimal>(type: "numeric(13,10)", nullable: false),
                    has_ewt = table.Column<bool>(type: "boolean", nullable: false),
                    ewt_rate = table.Column<decimal>(type: "numeric(7,4)", nullable: false),
                    has_trust_fee = table.Column<bool>(type: "boolean", nullable: false),
                    trust_fee_rate = table.Column<decimal>(type: "numeric(11,8)", nullable: false),
                    interest_deposited = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    interest_deposited_to = table.Column<string>(type: "varchar(100)", nullable: true),
                    interest_deposited_date = table.Column<DateOnly>(type: "date", nullable: true),
                    frequency_of_payment = table.Column<string>(type: "varchar(20)", nullable: true),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    posted_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    terminated_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    terminated_date = table.Column<DateOnly>(type: "date", nullable: true),
                    termination_remarks = table.Column<string>(type: "varchar(255)", nullable: true),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false),
                    locked_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    interest_status = table.Column<string>(type: "varchar(50)", nullable: true),
                    td_account_number = table.Column<string>(type: "varchar(50)", nullable: false),
                    is_posted = table.Column<bool>(type: "boolean", nullable: false),
                    batch_number = table.Column<string>(type: "varchar(50)", nullable: true),
                    status = table.Column<string>(type: "varchar(50)", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: false),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_rolled = table.Column<bool>(type: "boolean", nullable: false),
                    rolled_from_id = table.Column<int>(type: "integer", nullable: true),
                    is_swapped = table.Column<bool>(type: "boolean", nullable: false),
                    swapped_from_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bienes_placements", x => x.placement_id);
                    table.ForeignKey(
                        name: "fk_bienes_placements_bienes_placements_rolled_from_id",
                        column: x => x.rolled_from_id,
                        principalTable: "bienes_placements",
                        principalColumn: "placement_id");
                    table.ForeignKey(
                        name: "fk_bienes_placements_bienes_placements_swapped_from_id",
                        column: x => x.swapped_from_id,
                        principalTable: "bienes_placements",
                        principalColumn: "placement_id");
                    table.ForeignKey(
                        name: "fk_bienes_placements_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "company_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bienes_placements_filpride_bank_accounts_bank_id",
                        column: x => x.bank_id,
                        principalTable: "filpride_bank_accounts",
                        principalColumn: "bank_account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bienes_placements_filpride_bank_accounts_settlement_account",
                        column: x => x.settlement_account_id,
                        principalTable: "filpride_bank_accounts",
                        principalColumn: "bank_account_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "filpride_customer_branches",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    branch_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    branch_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    branch_tin = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_customer_branches", x => x.id);
                    table.ForeignKey(
                        name: "fk_filpride_customer_branches_filpride_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "filpride_customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_collections",
                columns: table => new
                {
                    mmsi_collection_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    mmsi_collection_number = table.Column<string>(type: "text", nullable: false),
                    check_number = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    check_date = table.Column<DateOnly>(type: "date", nullable: false),
                    deposit_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    ewt = table.Column<decimal>(type: "numeric", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    is_undocumented = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_collections", x => x.mmsi_collection_id);
                    table.ForeignKey(
                        name: "fk_mmsi_collections_filpride_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "filpride_customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_principals",
                columns: table => new
                {
                    principal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    principal_number = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: false),
                    principal_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    address = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    business_type = table.Column<string>(type: "text", nullable: true),
                    terms = table.Column<string>(type: "text", nullable: true),
                    tin = table.Column<string>(type: "text", nullable: true),
                    landline1 = table.Column<string>(type: "text", nullable: true),
                    landline2 = table.Column<string>(type: "text", nullable: true),
                    mobile1 = table.Column<string>(type: "text", nullable: true),
                    mobile2 = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_vatable = table.Column<bool>(type: "boolean", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_principals", x => x.principal_id);
                    table.ForeignKey(
                        name: "fk_mmsi_principals_filpride_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "filpride_customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "filpride_check_voucher_headers",
                columns: table => new
                {
                    check_voucher_header_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    check_voucher_header_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    old_cv_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    rr_no = table.Column<string[]>(type: "varchar[]", nullable: true),
                    si_no = table.Column<string[]>(type: "varchar[]", nullable: true),
                    po_no = table.Column<string[]>(type: "varchar[]", nullable: true),
                    supplier_id = table.Column<int>(type: "integer", nullable: true),
                    supplier_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount = table.Column<decimal[]>(type: "numeric(18,4)[]", nullable: true),
                    particulars = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    bank_id = table.Column<int>(type: "integer", nullable: true),
                    bank_account_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    bank_account_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    check_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    payee = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    check_date = table.Column<DateOnly>(type: "date", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    number_of_months = table.Column<int>(type: "integer", nullable: false),
                    number_of_months_created = table.Column<int>(type: "integer", nullable: false),
                    last_created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    amount_per_month = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_complete = table.Column<bool>(type: "boolean", nullable: false),
                    accrued_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reference = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    cv_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    check_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    invoice_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    supporting_file_saved_file_name = table.Column<string>(type: "text", nullable: true),
                    supporting_file_saved_url = table.Column<string>(type: "text", nullable: true),
                    dcp_date = table.Column<DateOnly>(type: "date", nullable: true),
                    dcr_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_advances = table.Column<bool>(type: "boolean", nullable: false),
                    employee_id = table.Column<int>(type: "integer", nullable: true),
                    address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("pk_filpride_check_voucher_headers", x => x.check_voucher_header_id);
                    table.ForeignKey(
                        name: "fk_filpride_check_voucher_headers_filpride_bank_accounts_bank_",
                        column: x => x.bank_id,
                        principalTable: "filpride_bank_accounts",
                        principalColumn: "bank_account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_check_voucher_headers_filpride_employees_employee_",
                        column: x => x.employee_id,
                        principalTable: "filpride_employees",
                        principalColumn: "employee_id");
                    table.ForeignKey(
                        name: "fk_filpride_check_voucher_headers_filpride_suppliers_supplier_",
                        column: x => x.supplier_id,
                        principalTable: "filpride_suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Restrict);
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
                    is_posted = table.Column<bool>(type: "boolean", nullable: false),
                    company = table.Column<string>(type: "text", nullable: false),
                    bank_account_id = table.Column<int>(type: "integer", nullable: true),
                    bank_account_name = table.Column<string>(type: "text", nullable: true),
                    supplier_id = table.Column<int>(type: "integer", nullable: true),
                    supplier_name = table.Column<string>(type: "text", nullable: true),
                    account_id = table.Column<int>(type: "integer", nullable: true),
                    employee_id = table.Column<int>(type: "integer", nullable: true),
                    employee_name = table.Column<string>(type: "text", nullable: true),
                    customer_id = table.Column<int>(type: "integer", nullable: true),
                    customer_name = table.Column<string>(type: "text", nullable: true),
                    company_id = table.Column<int>(type: "integer", nullable: true),
                    company_name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_general_ledger_books", x => x.general_ledger_book_id);
                    table.ForeignKey(
                        name: "fk_filpride_general_ledger_books_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "company_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_general_ledger_books_filpride_bank_accounts_bank_a",
                        column: x => x.bank_account_id,
                        principalTable: "filpride_bank_accounts",
                        principalColumn: "bank_account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_general_ledger_books_filpride_chart_of_accounts_ac",
                        column: x => x.account_id,
                        principalTable: "filpride_chart_of_accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_general_ledger_books_filpride_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "filpride_customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_general_ledger_books_filpride_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "filpride_employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_general_ledger_books_filpride_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "filpride_suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "filpride_pick_up_points",
                columns: table => new
                {
                    pick_up_point_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    depot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    company = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_filpride = table.Column<bool>(type: "boolean", nullable: false),
                    is_mobility = table.Column<bool>(type: "boolean", nullable: false),
                    is_bienes = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_pick_up_points", x => x.pick_up_point_id);
                    table.ForeignKey(
                        name: "fk_filpride_pick_up_points_filpride_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "filpride_suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_terminals",
                columns: table => new
                {
                    terminal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    terminal_number = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: true),
                    terminal_name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    port_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_terminals", x => x.terminal_id);
                    table.ForeignKey(
                        name: "fk_mmsi_terminals_mmsi_ports_port_id",
                        column: x => x.port_id,
                        principalTable: "mmsi_ports",
                        principalColumn: "port_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_tugboats",
                columns: table => new
                {
                    tugboat_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tugboat_number = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false),
                    tugboat_name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    is_company_owned = table.Column<bool>(type: "boolean", nullable: false),
                    tugboat_owner_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_tugboats", x => x.tugboat_id);
                    table.ForeignKey(
                        name: "fk_mmsi_tugboats_mmsi_tugboat_owners_tugboat_owner_id",
                        column: x => x.tugboat_owner_id,
                        principalTable: "mmsi_tugboat_owners",
                        principalColumn: "tugboat_owner_id");
                });

            migrationBuilder.CreateTable(
                name: "mobility_lube_purchase_details",
                columns: table => new
                {
                    lube_purchase_detail_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lube_purchase_header_id = table.Column<int>(type: "integer", nullable: false),
                    lube_purchase_header_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit = table.Column<string>(type: "varchar(10)", nullable: false),
                    description = table.Column<string>(type: "varchar(200)", nullable: false),
                    cost_per_case = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    cost_per_piece = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    product_code = table.Column<string>(type: "varchar(10)", nullable: false),
                    piece = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    station_code = table.Column<string>(type: "varchar(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_lube_purchase_details", x => x.lube_purchase_detail_id);
                    table.ForeignKey(
                        name: "fk_mobility_lube_purchase_details_mobility_lube_purchase_heade",
                        column: x => x.lube_purchase_header_id,
                        principalTable: "mobility_lube_purchase_headers",
                        principalColumn: "lube_purchase_header_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mobility_sales_details",
                columns: table => new
                {
                    sales_detail_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sales_header_id = table.Column<int>(type: "integer", nullable: false),
                    sales_no = table.Column<string>(type: "varchar(15)", nullable: false),
                    product = table.Column<string>(type: "varchar(20)", nullable: false),
                    particular = table.Column<string>(type: "varchar(50)", nullable: false),
                    closing = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    opening = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    liters = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    calibration = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    liters_sold = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    transaction_count = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    sale = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    value = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    reference_no = table.Column<string>(type: "varchar(15)", nullable: true),
                    station_code = table.Column<string>(type: "varchar(3)", nullable: false),
                    previous_price = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    pump_number = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_sales_details", x => x.sales_detail_id);
                    table.ForeignKey(
                        name: "fk_mobility_sales_details_mobility_sales_headers_sales_header_",
                        column: x => x.sales_header_id,
                        principalTable: "mobility_sales_headers",
                        principalColumn: "sales_header_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mobility_service_invoices",
                columns: table => new
                {
                    service_invoice_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_invoice_no = table.Column<string>(type: "varchar(12)", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    customer_address = table.Column<string>(type: "text", nullable: false),
                    customer_tin = table.Column<string>(type: "text", nullable: false),
                    service_id = table.Column<int>(type: "integer", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    period = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    discount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    current_and_previous_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unearned_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    payment_status = table.Column<string>(type: "varchar(20)", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    instructions = table.Column<string>(type: "varchar(200)", nullable: true),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    station_code = table.Column<string>(type: "varchar(3)", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_mobility_service_invoices", x => x.service_invoice_id);
                    table.ForeignKey(
                        name: "fk_mobility_service_invoices_mobility_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "mobility_customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mobility_service_invoices_mobility_services_service_id",
                        column: x => x.service_id,
                        principalTable: "mobility_services",
                        principalColumn: "service_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mobility_service_invoices_mobility_stations_station_code",
                        column: x => x.station_code,
                        principalTable: "mobility_stations",
                        principalColumn: "station_code",
                        onDelete: ReferentialAction.Cascade);
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
                    station_code = table.Column<string>(type: "text", nullable: false),
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
                        name: "fk_mobility_check_voucher_headers_mobility_station_employees_e",
                        column: x => x.employee_id,
                        principalTable: "mobility_station_employees",
                        principalColumn: "employee_id");
                    table.ForeignKey(
                        name: "fk_mobility_check_voucher_headers_mobility_suppliers_supplier_",
                        column: x => x.supplier_id,
                        principalTable: "mobility_suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mobility_pick_up_points",
                columns: table => new
                {
                    pick_up_point_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    depot = table.Column<string>(type: "varchar(50)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    station_code = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_pick_up_points", x => x.pick_up_point_id);
                    table.ForeignKey(
                        name: "fk_mobility_pick_up_points_mobility_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "mobility_suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_notifications",
                columns: table => new
                {
                    user_notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    requires_response = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_notifications", x => x.user_notification_id);
                    table.ForeignKey(
                        name: "fk_user_notifications_application_user_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_notifications_notifications_notification_id",
                        column: x => x.notification_id,
                        principalTable: "notifications",
                        principalColumn: "notification_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mobility_customer_order_slips",
                columns: table => new
                {
                    customer_order_slip_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_order_slip_no = table.Column<string>(type: "varchar(13)", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    price_per_liter = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    address = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    plate_no = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    driver = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "varchar(20)", nullable: false),
                    load_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    station_code = table.Column<string>(type: "varchar(3)", nullable: false),
                    terms = table.Column<string>(type: "varchar(10)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    disapproved_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    disapproved_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    approved_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    uploaded_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    uploaded_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    trip_ticket = table.Column<string>(type: "varchar(20)", nullable: true),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    disapproval_remarks = table.Column<string>(type: "varchar(200)", nullable: true),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    station_id = table.Column<int>(type: "integer", nullable: false),
                    saved_url = table.Column<string>(type: "text", nullable: true),
                    saved_file_name = table.Column<string>(type: "text", nullable: true),
                    check_picture_saved_url = table.Column<string>(type: "text", nullable: true),
                    check_picture_saved_file_name = table.Column<string>(type: "text", nullable: true),
                    check_no = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_customer_order_slips", x => x.customer_order_slip_id);
                    table.ForeignKey(
                        name: "fk_mobility_customer_order_slips_mobility_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "mobility_customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mobility_customer_order_slips_mobility_stations_station_code",
                        column: x => x.station_code,
                        principalTable: "mobility_stations",
                        principalColumn: "station_code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mobility_customer_order_slips_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                    station_code = table.Column<string>(type: "varchar(5)", nullable: false),
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
                        column: x => x.station_code,
                        principalTable: "mobility_stations",
                        principalColumn: "station_code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mobility_customer_purchase_orders_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "filpride_check_voucher_details",
                columns: table => new
                {
                    check_voucher_detail_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    account_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    transaction_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    check_voucher_header_id = table.Column<int>(type: "integer", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_vatable = table.Column<bool>(type: "boolean", nullable: false),
                    ewt_percent = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_user_selected = table.Column<bool>(type: "boolean", nullable: false),
                    bank_id = table.Column<int>(type: "integer", nullable: true),
                    company_id = table.Column<int>(type: "integer", nullable: true),
                    customer_id = table.Column<int>(type: "integer", nullable: true),
                    employee_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_check_voucher_details", x => x.check_voucher_detail_id);
                    table.ForeignKey(
                        name: "fk_filpride_check_voucher_details_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "company_id");
                    table.ForeignKey(
                        name: "fk_filpride_check_voucher_details_filpride_bank_accounts_bank_",
                        column: x => x.bank_id,
                        principalTable: "filpride_bank_accounts",
                        principalColumn: "bank_account_id");
                    table.ForeignKey(
                        name: "fk_filpride_check_voucher_details_filpride_check_voucher_heade",
                        column: x => x.check_voucher_header_id,
                        principalTable: "filpride_check_voucher_headers",
                        principalColumn: "check_voucher_header_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_check_voucher_details_filpride_customers_customer_",
                        column: x => x.customer_id,
                        principalTable: "filpride_customers",
                        principalColumn: "customer_id");
                    table.ForeignKey(
                        name: "fk_filpride_check_voucher_details_filpride_employees_employee_",
                        column: x => x.employee_id,
                        principalTable: "filpride_employees",
                        principalColumn: "employee_id");
                    table.ForeignKey(
                        name: "fk_filpride_check_voucher_details_filpride_suppliers_supplier_",
                        column: x => x.supplier_id,
                        principalTable: "filpride_suppliers",
                        principalColumn: "supplier_id");
                });

            migrationBuilder.CreateTable(
                name: "filpride_cv_trade_payments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    document_id = table.Column<int>(type: "integer", nullable: false),
                    document_type = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    check_voucher_id = table.Column<int>(type: "integer", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_cv_trade_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_filpride_cv_trade_payments_filpride_check_voucher_headers_c",
                        column: x => x.check_voucher_id,
                        principalTable: "filpride_check_voucher_headers",
                        principalColumn: "check_voucher_header_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "filpride_journal_voucher_headers",
                columns: table => new
                {
                    journal_voucher_header_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    journal_voucher_header_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    references = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    cv_id = table.Column<int>(type: "integer", nullable: true),
                    particulars = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    cr_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    jv_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
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
                    table.PrimaryKey("pk_filpride_journal_voucher_headers", x => x.journal_voucher_header_id);
                    table.ForeignKey(
                        name: "fk_filpride_journal_voucher_headers_filpride_check_voucher_hea",
                        column: x => x.cv_id,
                        principalTable: "filpride_check_voucher_headers",
                        principalColumn: "check_voucher_header_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "filpride_multiple_check_voucher_payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    check_voucher_header_payment_id = table.Column<int>(type: "integer", nullable: false),
                    check_voucher_header_invoice_id = table.Column<int>(type: "integer", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_multiple_check_voucher_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_filpride_multiple_check_voucher_payments_filpride_check_vou",
                        column: x => x.check_voucher_header_invoice_id,
                        principalTable: "filpride_check_voucher_headers",
                        principalColumn: "check_voucher_header_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_multiple_check_voucher_payments_filpride_check_vou1",
                        column: x => x.check_voucher_header_payment_id,
                        principalTable: "filpride_check_voucher_headers",
                        principalColumn: "check_voucher_header_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "filpride_freights",
                columns: table => new
                {
                    freight_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pick_up_point_id = table.Column<int>(type: "integer", nullable: false),
                    cluster_code = table.Column<int>(type: "integer", nullable: false),
                    freight = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_freights", x => x.freight_id);
                    table.ForeignKey(
                        name: "fk_filpride_freights_filpride_pick_up_points_pick_up_point_id",
                        column: x => x.pick_up_point_id,
                        principalTable: "filpride_pick_up_points",
                        principalColumn: "pick_up_point_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "filpride_purchase_orders",
                columns: table => new
                {
                    purchase_order_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    purchase_order_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    supplier_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    supplier_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    supplier_tin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    product_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    final_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    terms = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    quantity_received = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_received = table.Column<bool>(type: "boolean", nullable: false),
                    received_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    supplier_sales_order_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_closed = table.Column<bool>(type: "boolean", nullable: false),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_sub_po = table.Column<bool>(type: "boolean", nullable: false),
                    sub_po_series = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    customer_id = table.Column<int>(type: "integer", nullable: true),
                    old_po_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    trigger_date = table.Column<DateOnly>(type: "date", nullable: false),
                    un_triggered_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    pick_up_point_id = table.Column<int>(type: "integer", nullable: false),
                    vat_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    tax_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("pk_filpride_purchase_orders", x => x.purchase_order_id);
                    table.ForeignKey(
                        name: "fk_filpride_purchase_orders_filpride_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "filpride_customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_purchase_orders_filpride_pick_up_points_pick_up_po",
                        column: x => x.pick_up_point_id,
                        principalTable: "filpride_pick_up_points",
                        principalColumn: "pick_up_point_id",
                        onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.CreateTable(
                name: "mmsi_billings",
                columns: table => new
                {
                    mmsi_billing_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    mmsi_billing_number = table.Column<string>(type: "varchar(10)", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    is_undocumented = table.Column<bool>(type: "boolean", nullable: false),
                    billed_to = table.Column<string>(type: "varchar(10)", nullable: false),
                    voyage_number = table.Column<string>(type: "text", nullable: true),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_edited_by = table.Column<string>(type: "text", nullable: true),
                    last_edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_principal = table.Column<bool>(type: "boolean", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    principal_id = table.Column<int>(type: "integer", nullable: true),
                    vessel_id = table.Column<int>(type: "integer", nullable: false),
                    port_id = table.Column<int>(type: "integer", nullable: false),
                    terminal_id = table.Column<int>(type: "integer", nullable: false),
                    collection_id = table.Column<int>(type: "integer", nullable: true),
                    mmsi_collection_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_billings", x => x.mmsi_billing_id);
                    table.ForeignKey(
                        name: "fk_mmsi_billings_filpride_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "filpride_customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mmsi_billings_mmsi_collections_mmsi_collection_id",
                        column: x => x.mmsi_collection_id,
                        principalTable: "mmsi_collections",
                        principalColumn: "mmsi_collection_id");
                    table.ForeignKey(
                        name: "fk_mmsi_billings_mmsi_ports_port_id",
                        column: x => x.port_id,
                        principalTable: "mmsi_ports",
                        principalColumn: "port_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mmsi_billings_mmsi_principals_principal_id",
                        column: x => x.principal_id,
                        principalTable: "mmsi_principals",
                        principalColumn: "principal_id");
                    table.ForeignKey(
                        name: "fk_mmsi_billings_mmsi_terminals_terminal_id",
                        column: x => x.terminal_id,
                        principalTable: "mmsi_terminals",
                        principalColumn: "terminal_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mmsi_billings_mmsi_vessels_vessel_id",
                        column: x => x.vessel_id,
                        principalTable: "mmsi_vessels",
                        principalColumn: "vessel_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_tariff_rates",
                columns: table => new
                {
                    tariff_rate_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    as_of_date = table.Column<DateOnly>(type: "date", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    terminal_id = table.Column<int>(type: "integer", nullable: false),
                    service_id = table.Column<int>(type: "integer", nullable: false),
                    dispatch = table.Column<decimal>(type: "numeric", nullable: false),
                    baf = table.Column<decimal>(type: "numeric", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by = table.Column<string>(type: "text", nullable: true),
                    update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    dispatch_discount = table.Column<decimal>(type: "numeric", nullable: false),
                    baf_discount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_tariff_rates", x => x.tariff_rate_id);
                    table.ForeignKey(
                        name: "fk_mmsi_tariff_rates_filpride_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "filpride_customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mmsi_tariff_rates_mmsi_services_service_id",
                        column: x => x.service_id,
                        principalTable: "mmsi_services",
                        principalColumn: "service_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mmsi_tariff_rates_mmsi_terminals_terminal_id",
                        column: x => x.terminal_id,
                        principalTable: "mmsi_terminals",
                        principalColumn: "terminal_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_dispatch_tickets",
                columns: table => new
                {
                    dispatch_ticket_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    dispatch_number = table.Column<string>(type: "varchar(20)", nullable: false),
                    cos_number = table.Column<string>(type: "varchar(10)", nullable: true),
                    date_left = table.Column<DateOnly>(type: "date", nullable: false),
                    date_arrived = table.Column<DateOnly>(type: "date", nullable: false),
                    time_left = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    time_arrived = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    remarks = table.Column<string>(type: "varchar(100)", nullable: true),
                    base_or_station = table.Column<string>(type: "varchar(100)", nullable: true),
                    voyage_number = table.Column<string>(type: "varchar(100)", nullable: true),
                    dispatch_charge_type = table.Column<string>(type: "text", nullable: false),
                    baf_charge_type = table.Column<string>(type: "text", nullable: false),
                    total_hours = table.Column<decimal>(type: "numeric", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "text", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    dispatch_rate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    dispatch_billing_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    dispatch_discount = table.Column<decimal>(type: "numeric", nullable: false),
                    dispatch_net_revenue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    baf_rate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    baf_billing_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    baf_discount = table.Column<decimal>(type: "numeric", nullable: false),
                    baf_net_revenue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_billing = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_net_revenue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ap_other_tugs = table.Column<decimal>(type: "numeric", nullable: false),
                    tariff_by = table.Column<string>(type: "text", nullable: false),
                    tariff_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tariff_edited_by = table.Column<string>(type: "text", nullable: false),
                    tariff_edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    image_name = table.Column<string>(type: "text", nullable: true),
                    image_saved_url = table.Column<string>(type: "text", nullable: true),
                    image_signed_url = table.Column<string>(type: "text", nullable: true),
                    video_name = table.Column<string>(type: "text", nullable: true),
                    video_saved_url = table.Column<string>(type: "text", nullable: true),
                    video_signed_url = table.Column<string>(type: "text", nullable: true),
                    billing_id = table.Column<string>(type: "text", nullable: true),
                    customer_id = table.Column<int>(type: "integer", nullable: true),
                    tug_boat_id = table.Column<int>(type: "integer", nullable: false),
                    tug_master_id = table.Column<int>(type: "integer", nullable: false),
                    vessel_id = table.Column<int>(type: "integer", nullable: false),
                    terminal_id = table.Column<int>(type: "integer", nullable: false),
                    service_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_dispatch_tickets", x => x.dispatch_ticket_id);
                    table.ForeignKey(
                        name: "fk_mmsi_dispatch_tickets_filpride_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "filpride_customers",
                        principalColumn: "customer_id");
                    table.ForeignKey(
                        name: "fk_mmsi_dispatch_tickets_mmsi_services_service_id",
                        column: x => x.service_id,
                        principalTable: "mmsi_services",
                        principalColumn: "service_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mmsi_dispatch_tickets_mmsi_terminals_terminal_id",
                        column: x => x.terminal_id,
                        principalTable: "mmsi_terminals",
                        principalColumn: "terminal_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mmsi_dispatch_tickets_mmsi_tug_masters_tug_master_id",
                        column: x => x.tug_master_id,
                        principalTable: "mmsi_tug_masters",
                        principalColumn: "tug_master_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mmsi_dispatch_tickets_mmsi_tugboats_tug_boat_id",
                        column: x => x.tug_boat_id,
                        principalTable: "mmsi_tugboats",
                        principalColumn: "tugboat_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mmsi_dispatch_tickets_mmsi_vessels_vessel_id",
                        column: x => x.vessel_id,
                        principalTable: "mmsi_vessels",
                        principalColumn: "vessel_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mobility_collection_receipts",
                columns: table => new
                {
                    collection_receipt_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    collection_receipt_no = table.Column<string>(type: "varchar(12)", nullable: true),
                    service_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    sv_no = table.Column<string>(type: "varchar(12)", nullable: true),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    series_number = table.Column<long>(type: "bigint", nullable: false),
                    reference_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    remarks = table.Column<string>(type: "varchar(100)", nullable: true),
                    cash_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    check_date = table.Column<string>(type: "text", nullable: true),
                    check_no = table.Column<string>(type: "varchar(50)", nullable: true),
                    check_bank = table.Column<string>(type: "varchar(50)", nullable: true),
                    check_branch = table.Column<string>(type: "varchar(50)", nullable: true),
                    check_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    manager_check_date = table.Column<DateOnly>(type: "date", nullable: true),
                    manager_check_no = table.Column<string>(type: "varchar(50)", nullable: true),
                    manager_check_bank = table.Column<string>(type: "varchar(50)", nullable: true),
                    manager_check_branch = table.Column<string>(type: "varchar(50)", nullable: true),
                    manager_check_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ewt = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    wvat = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_certificate_upload = table.Column<bool>(type: "boolean", nullable: false),
                    f2306file_path = table.Column<string>(type: "varchar(200)", nullable: true),
                    f2306file_name = table.Column<string>(type: "text", nullable: true),
                    f2307file_path = table.Column<string>(type: "varchar(200)", nullable: true),
                    f2307file_name = table.Column<string>(type: "text", nullable: true),
                    station_code = table.Column<string>(type: "text", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_mobility_collection_receipts", x => x.collection_receipt_id);
                    table.ForeignKey(
                        name: "fk_mobility_collection_receipts_mobility_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "mobility_customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mobility_collection_receipts_mobility_service_invoices_serv",
                        column: x => x.service_invoice_id,
                        principalTable: "mobility_service_invoices",
                        principalColumn: "service_invoice_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mobility_credit_memos",
                columns: table => new
                {
                    credit_memo_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    credit_memo_no = table.Column<string>(type: "varchar(12)", nullable: true),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    service_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    description = table.Column<string>(type: "text", nullable: false),
                    credit_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    remarks = table.Column<string>(type: "text", nullable: true),
                    period = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    current_and_previous_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unearned_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    station_code = table.Column<string>(type: "text", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_mobility_credit_memos", x => x.credit_memo_id);
                    table.ForeignKey(
                        name: "fk_mobility_credit_memos_mobility_service_invoices_service_inv",
                        column: x => x.service_invoice_id,
                        principalTable: "mobility_service_invoices",
                        principalColumn: "service_invoice_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mobility_debit_memos",
                columns: table => new
                {
                    debit_memo_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    debit_memo_no = table.Column<string>(type: "varchar(12)", nullable: true),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    debit_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    remarks = table.Column<string>(type: "text", nullable: false),
                    period = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    current_and_previous_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unearned_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    station_code = table.Column<string>(type: "text", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_mobility_debit_memos", x => x.debit_memo_id);
                    table.ForeignKey(
                        name: "fk_mobility_debit_memos_mobility_service_invoices_service_invo",
                        column: x => x.service_invoice_id,
                        principalTable: "mobility_service_invoices",
                        principalColumn: "service_invoice_id",
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
                    employee_id = table.Column<int>(type: "integer", nullable: true),
                    station_id = table.Column<int>(type: "integer", nullable: true)
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
                name: "mobility_journal_voucher_headers",
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
                    station_code = table.Column<string>(type: "text", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_mobility_journal_voucher_headers", x => x.journal_voucher_header_id);
                    table.ForeignKey(
                        name: "fk_mobility_journal_voucher_headers_mobility_check_voucher_hea",
                        column: x => x.cv_id,
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

            migrationBuilder.CreateTable(
                name: "mobility_purchase_orders",
                columns: table => new
                {
                    purchase_order_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    purchase_order_no = table.Column<string>(type: "varchar(15)", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    supplier_address = table.Column<string>(type: "text", nullable: false),
                    supplier_tin = table.Column<string>(type: "text", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    final_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    remarks = table.Column<string>(type: "varchar(200)", nullable: false),
                    terms = table.Column<string>(type: "varchar(10)", nullable: false),
                    quantity_received = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_received = table.Column<bool>(type: "boolean", nullable: false),
                    received_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    supplier_sales_order_no = table.Column<string>(type: "varchar(100)", nullable: true),
                    is_closed = table.Column<bool>(type: "boolean", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    pick_up_point_id = table.Column<int>(type: "integer", nullable: false),
                    discount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    station_code = table.Column<string>(type: "varchar(3)", nullable: false),
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
                    table.PrimaryKey("pk_mobility_purchase_orders", x => x.purchase_order_id);
                    table.ForeignKey(
                        name: "fk_mobility_purchase_orders_mobility_pick_up_points_pick_up_po",
                        column: x => x.pick_up_point_id,
                        principalTable: "mobility_pick_up_points",
                        principalColumn: "pick_up_point_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mobility_purchase_orders_mobility_products_product_id",
                        column: x => x.product_id,
                        principalTable: "mobility_products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mobility_purchase_orders_mobility_stations_station_code",
                        column: x => x.station_code,
                        principalTable: "mobility_stations",
                        principalColumn: "station_code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mobility_purchase_orders_mobility_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "mobility_suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "filpride_journal_voucher_details",
                columns: table => new
                {
                    journal_voucher_detail_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    account_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    transaction_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    journal_voucher_header_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_journal_voucher_details", x => x.journal_voucher_detail_id);
                    table.ForeignKey(
                        name: "fk_filpride_journal_voucher_details_filpride_journal_voucher_h",
                        column: x => x.journal_voucher_header_id,
                        principalTable: "filpride_journal_voucher_headers",
                        principalColumn: "journal_voucher_header_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "filpride_customer_order_slips",
                columns: table => new
                {
                    customer_order_slip_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_order_slip_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    customer_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    customer_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    customer_tin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    customer_po_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    delivered_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    delivered_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    balance_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    has_commission = table.Column<bool>(type: "boolean", nullable: false),
                    commissionee_id = table.Column<int>(type: "integer", nullable: true),
                    commission_rate = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    account_specialist = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    branch = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    purchase_order_id = table.Column<int>(type: "integer", nullable: true),
                    delivery_option = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    freight = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    pick_up_point_id = table.Column<int>(type: "integer", nullable: true),
                    supplier_id = table.Column<int>(type: "integer", nullable: true),
                    sub_po_remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    om_approved_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    om_approved_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expiration_date = table.Column<DateOnly>(type: "date", nullable: true),
                    om_reason = table.Column<string>(type: "text", nullable: true),
                    cnc_approved_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    cnc_approved_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fm_approved_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    fm_approved_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    terms = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    finance_instruction = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    hauler_id = table.Column<int>(type: "integer", nullable: true),
                    driver = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    plate_no = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    authority_to_load_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_delivered = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    disapproved_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    disapproved_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    old_cos_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    has_multiple_po = table.Column<bool>(type: "boolean", nullable: false),
                    uploaded_files = table.Column<string[]>(type: "varchar[]", nullable: true),
                    old_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    price_reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    product_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    available_credit_limit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    vat_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    has_ewt = table.Column<bool>(type: "boolean", nullable: false),
                    has_wvat = table.Column<bool>(type: "boolean", nullable: false),
                    depot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    commissionee_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    business_style = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    commissionee_vat_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    commissionee_tax_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_customer_order_slips", x => x.customer_order_slip_id);
                    table.ForeignKey(
                        name: "fk_filpride_customer_order_slips_filpride_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "filpride_customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_customer_order_slips_filpride_pick_up_points_pick_",
                        column: x => x.pick_up_point_id,
                        principalTable: "filpride_pick_up_points",
                        principalColumn: "pick_up_point_id");
                    table.ForeignKey(
                        name: "fk_filpride_customer_order_slips_filpride_purchase_orders_purc",
                        column: x => x.purchase_order_id,
                        principalTable: "filpride_purchase_orders",
                        principalColumn: "purchase_order_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_customer_order_slips_filpride_suppliers_commission",
                        column: x => x.commissionee_id,
                        principalTable: "filpride_suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_customer_order_slips_filpride_suppliers_hauler_id",
                        column: x => x.hauler_id,
                        principalTable: "filpride_suppliers",
                        principalColumn: "supplier_id");
                    table.ForeignKey(
                        name: "fk_filpride_customer_order_slips_filpride_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "filpride_suppliers",
                        principalColumn: "supplier_id");
                    table.ForeignKey(
                        name: "fk_filpride_customer_order_slips_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "filpride_inventories",
                columns: table => new
                {
                    inventory_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    particular = table.Column<string>(type: "varchar(200)", nullable: false),
                    reference = table.Column<string>(type: "varchar(12)", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    inventory_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    average_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit = table.Column<string>(type: "varchar(2)", nullable: false),
                    is_validated = table.Column<bool>(type: "boolean", nullable: false),
                    validated_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    validated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    po_id = table.Column<int>(type: "integer", nullable: true),
                    company = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_inventories", x => x.inventory_id);
                    table.ForeignKey(
                        name: "fk_filpride_inventories_filpride_purchase_orders_po_id",
                        column: x => x.po_id,
                        principalTable: "filpride_purchase_orders",
                        principalColumn: "purchase_order_id");
                    table.ForeignKey(
                        name: "fk_filpride_inventories_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "filpride_po_actual_prices",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    purchase_order_id = table.Column<int>(type: "integer", nullable: false),
                    triggered_volume = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    applied_volume = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    triggered_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_approved = table.Column<bool>(type: "boolean", nullable: false),
                    approved_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approved_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    triggered_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_po_actual_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_filpride_po_actual_prices_filpride_purchase_orders_purchase",
                        column: x => x.purchase_order_id,
                        principalTable: "filpride_purchase_orders",
                        principalColumn: "purchase_order_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mobility_journal_voucher_details",
                columns: table => new
                {
                    journal_voucher_detail_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_no = table.Column<string>(type: "text", nullable: false),
                    account_name = table.Column<string>(type: "text", nullable: false),
                    transaction_no = table.Column<string>(type: "text", nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    journal_voucher_header_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_journal_voucher_details", x => x.journal_voucher_detail_id);
                    table.ForeignKey(
                        name: "fk_mobility_journal_voucher_details_mobility_journal_voucher_h",
                        column: x => x.journal_voucher_header_id,
                        principalTable: "mobility_journal_voucher_headers",
                        principalColumn: "journal_voucher_header_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mobility_receiving_reports",
                columns: table => new
                {
                    receiving_report_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    receiving_report_no = table.Column<string>(type: "varchar(15)", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    remarks = table.Column<string>(type: "varchar(200)", nullable: false),
                    station_code = table.Column<string>(type: "varchar(3)", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    purchase_order_id = table.Column<int>(type: "integer", nullable: false),
                    purchase_order_no = table.Column<string>(type: "varchar(15)", nullable: true),
                    supplier_invoice_number = table.Column<string>(type: "varchar(100)", nullable: true),
                    supplier_invoice_date = table.Column<DateOnly>(type: "date", nullable: true),
                    supplier_dr_no = table.Column<string>(type: "varchar(50)", nullable: true),
                    withdrawal_certificate = table.Column<string>(type: "varchar(50)", nullable: true),
                    truck_or_vessels = table.Column<string>(type: "varchar(100)", nullable: false),
                    quantity_delivered = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    quantity_received = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    gain_or_loss = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    authority_to_load_no = table.Column<string>(type: "varchar(100)", nullable: true),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    paid_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    canceled_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    received_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_mobility_receiving_reports", x => x.receiving_report_id);
                    table.ForeignKey(
                        name: "fk_mobility_receiving_reports_mobility_purchase_orders_purchas",
                        column: x => x.purchase_order_id,
                        principalTable: "mobility_purchase_orders",
                        principalColumn: "purchase_order_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "filpride_authority_to_loads",
                columns: table => new
                {
                    authority_to_load_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    authority_to_load_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    customer_order_slip_id = table.Column<int>(type: "integer", nullable: true),
                    date_booked = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_until = table.Column<DateOnly>(type: "date", nullable: false),
                    uppi_atl_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    remarks = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_by = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    hauler_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    driver = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    plate_no = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    supplier_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    depot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    load_port_id = table.Column<int>(type: "integer", nullable: false),
                    freight = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_authority_to_loads", x => x.authority_to_load_id);
                    table.ForeignKey(
                        name: "fk_filpride_authority_to_loads_filpride_customer_order_slips_c",
                        column: x => x.customer_order_slip_id,
                        principalTable: "filpride_customer_order_slips",
                        principalColumn: "customer_order_slip_id");
                    table.ForeignKey(
                        name: "fk_filpride_authority_to_loads_filpride_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "filpride_suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "filpride_cos_appointed_suppliers",
                columns: table => new
                {
                    sequence_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_order_slip_id = table.Column<int>(type: "integer", nullable: false),
                    purchase_order_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unserved_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_assigned_to_dr = table.Column<bool>(type: "boolean", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    atl_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    unreserved_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_cos_appointed_suppliers", x => x.sequence_id);
                    table.ForeignKey(
                        name: "fk_filpride_cos_appointed_suppliers_filpride_customer_order_sl",
                        column: x => x.customer_order_slip_id,
                        principalTable: "filpride_customer_order_slips",
                        principalColumn: "customer_order_slip_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_cos_appointed_suppliers_filpride_purchase_orders_p",
                        column: x => x.purchase_order_id,
                        principalTable: "filpride_purchase_orders",
                        principalColumn: "purchase_order_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_cos_appointed_suppliers_filpride_suppliers_supplie",
                        column: x => x.supplier_id,
                        principalTable: "filpride_suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "filpride_delivery_receipts",
                columns: table => new
                {
                    delivery_receipt_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    delivery_receipt_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    delivered_date = table.Column<DateOnly>(type: "date", nullable: true),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    customer_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    customer_tin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    customer_order_slip_id = table.Column<int>(type: "integer", nullable: false),
                    remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    manual_dr_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    hauler_id = table.Column<int>(type: "integer", nullable: true),
                    driver = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    plate_no = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    freight = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ecc = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    authority_to_load_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    has_already_invoiced = table.Column<bool>(type: "boolean", nullable: false),
                    purchase_order_id = table.Column<int>(type: "integer", nullable: true),
                    freight_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    commissionee_id = table.Column<int>(type: "integer", nullable: true),
                    commission_rate = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    commission_amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    freight_amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_commission_paid = table.Column<bool>(type: "boolean", nullable: false),
                    is_freight_paid = table.Column<bool>(type: "boolean", nullable: false),
                    commission_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    has_receiving_report = table.Column<bool>(type: "boolean", nullable: false),
                    hauler_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    hauler_vat_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    hauler_tax_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    authority_to_load_id = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_filpride_delivery_receipts", x => x.delivery_receipt_id);
                    table.ForeignKey(
                        name: "fk_filpride_delivery_receipts_filpride_authority_to_loads_auth",
                        column: x => x.authority_to_load_id,
                        principalTable: "filpride_authority_to_loads",
                        principalColumn: "authority_to_load_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_delivery_receipts_filpride_customer_order_slips_cu",
                        column: x => x.customer_order_slip_id,
                        principalTable: "filpride_customer_order_slips",
                        principalColumn: "customer_order_slip_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_delivery_receipts_filpride_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "filpride_customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_delivery_receipts_filpride_purchase_orders_purchas",
                        column: x => x.purchase_order_id,
                        principalTable: "filpride_purchase_orders",
                        principalColumn: "purchase_order_id");
                    table.ForeignKey(
                        name: "fk_filpride_delivery_receipts_filpride_suppliers_commissionee_",
                        column: x => x.commissionee_id,
                        principalTable: "filpride_suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_delivery_receipts_filpride_suppliers_hauler_id",
                        column: x => x.hauler_id,
                        principalTable: "filpride_suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "filpride_book_atl_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    authority_to_load_id = table.Column<int>(type: "integer", nullable: false),
                    customer_order_slip_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unserved_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    appointed_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_book_atl_details", x => x.id);
                    table.ForeignKey(
                        name: "fk_filpride_book_atl_details_filpride_authority_to_loads_autho",
                        column: x => x.authority_to_load_id,
                        principalTable: "filpride_authority_to_loads",
                        principalColumn: "authority_to_load_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_book_atl_details_filpride_cos_appointed_suppliers_",
                        column: x => x.appointed_id,
                        principalTable: "filpride_cos_appointed_suppliers",
                        principalColumn: "sequence_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_filpride_book_atl_details_filpride_customer_order_slips_cus",
                        column: x => x.customer_order_slip_id,
                        principalTable: "filpride_customer_order_slips",
                        principalColumn: "customer_order_slip_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "filpride_receiving_reports",
                columns: table => new
                {
                    receiving_report_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    receiving_report_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    po_id = table.Column<int>(type: "integer", nullable: false),
                    po_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    supplier_invoice_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    supplier_invoice_date = table.Column<DateOnly>(type: "date", nullable: true),
                    supplier_dr_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    withdrawal_certificate = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    truck_or_vessels = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    quantity_delivered = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    quantity_received = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    gain_or_loss = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    authority_to_load_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    paid_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    canceled_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    received_date = table.Column<DateOnly>(type: "date", nullable: true),
                    delivery_receipt_id = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    is_cost_updated = table.Column<bool>(type: "boolean", nullable: false),
                    old_rr_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("pk_filpride_receiving_reports", x => x.receiving_report_id);
                    table.ForeignKey(
                        name: "fk_filpride_receiving_reports_filpride_delivery_receipts_deliv",
                        column: x => x.delivery_receipt_id,
                        principalTable: "filpride_delivery_receipts",
                        principalColumn: "delivery_receipt_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_receiving_reports_filpride_purchase_orders_po_id",
                        column: x => x.po_id,
                        principalTable: "filpride_purchase_orders",
                        principalColumn: "purchase_order_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "filpride_sales_invoices",
                columns: table => new
                {
                    sales_invoice_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sales_invoice_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    customer_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    customer_tin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    other_ref_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    payment_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    discount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    is_tax_and_vat_paid = table.Column<bool>(type: "boolean", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    purchase_order_id = table.Column<int>(type: "integer", nullable: false),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    type = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    receiving_report_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    delivery_receipt_id = table.Column<int>(type: "integer", nullable: true),
                    customer_order_slip_id = table.Column<int>(type: "integer", nullable: true),
                    terms = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
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
                    table.PrimaryKey("pk_filpride_sales_invoices", x => x.sales_invoice_id);
                    table.ForeignKey(
                        name: "fk_filpride_sales_invoices_filpride_customer_order_slips_custo",
                        column: x => x.customer_order_slip_id,
                        principalTable: "filpride_customer_order_slips",
                        principalColumn: "customer_order_slip_id");
                    table.ForeignKey(
                        name: "fk_filpride_sales_invoices_filpride_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "filpride_customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_sales_invoices_filpride_delivery_receipts_delivery",
                        column: x => x.delivery_receipt_id,
                        principalTable: "filpride_delivery_receipts",
                        principalColumn: "delivery_receipt_id");
                    table.ForeignKey(
                        name: "fk_filpride_sales_invoices_filpride_purchase_orders_purchase_o",
                        column: x => x.purchase_order_id,
                        principalTable: "filpride_purchase_orders",
                        principalColumn: "purchase_order_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_sales_invoices_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "filpride_sales_locked_records_queues",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    locked_date = table.Column<DateOnly>(type: "date", nullable: false),
                    delivery_receipt_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_sales_locked_records_queues", x => x.id);
                    table.ForeignKey(
                        name: "fk_filpride_sales_locked_records_queues_filpride_delivery_rece",
                        column: x => x.delivery_receipt_id,
                        principalTable: "filpride_delivery_receipts",
                        principalColumn: "delivery_receipt_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "filpride_service_invoices",
                columns: table => new
                {
                    service_invoice_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_invoice_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    customer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    customer_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    customer_tin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    customer_business_style = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    service_id = table.Column<int>(type: "integer", nullable: false),
                    service_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    service_percent = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    period = table.Column<DateOnly>(type: "date", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    discount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    current_and_previous_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unearned_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    payment_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    instructions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    vat_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    has_ewt = table.Column<bool>(type: "boolean", nullable: false),
                    has_wvat = table.Column<bool>(type: "boolean", nullable: false),
                    delivery_receipt_id = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("pk_filpride_service_invoices", x => x.service_invoice_id);
                    table.ForeignKey(
                        name: "fk_filpride_service_invoices_filpride_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "filpride_customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_service_invoices_filpride_delivery_receipts_delive",
                        column: x => x.delivery_receipt_id,
                        principalTable: "filpride_delivery_receipts",
                        principalColumn: "delivery_receipt_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_service_invoices_filpride_services_service_id",
                        column: x => x.service_id,
                        principalTable: "filpride_services",
                        principalColumn: "service_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "filpride_purchase_locked_records_queues",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    locked_date = table.Column<DateOnly>(type: "date", nullable: false),
                    receiving_report_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_purchase_locked_records_queues", x => x.id);
                    table.ForeignKey(
                        name: "fk_filpride_purchase_locked_records_queues_filpride_receiving_",
                        column: x => x.receiving_report_id,
                        principalTable: "filpride_receiving_reports",
                        principalColumn: "receiving_report_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "filpride_collection_receipts",
                columns: table => new
                {
                    collection_receipt_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    collection_receipt_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    sales_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    si_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    multiple_si_id = table.Column<int[]>(type: "integer[]", nullable: true),
                    multiple_si = table.Column<string[]>(type: "text[]", nullable: true),
                    service_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    sv_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reference_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    remarks = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    cash_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    check_date = table.Column<DateOnly>(type: "date", nullable: true),
                    check_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    bank_id = table.Column<int>(type: "integer", nullable: true),
                    check_branch = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    bank_account_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    bank_account_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    check_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ewt = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    wvat = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_certificate_upload = table.Column<bool>(type: "boolean", nullable: false),
                    f2306file_path = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    f2306file_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    f2307file_path = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    f2307file_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    si_multiple_amount = table.Column<decimal[]>(type: "numeric[]", nullable: true),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    multiple_transaction_date = table.Column<DateOnly[]>(type: "date[]", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
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
                    table.PrimaryKey("pk_filpride_collection_receipts", x => x.collection_receipt_id);
                    table.ForeignKey(
                        name: "fk_filpride_collection_receipts_filpride_bank_accounts_bank_id",
                        column: x => x.bank_id,
                        principalTable: "filpride_bank_accounts",
                        principalColumn: "bank_account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_collection_receipts_filpride_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "filpride_customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_collection_receipts_filpride_sales_invoices_sales_",
                        column: x => x.sales_invoice_id,
                        principalTable: "filpride_sales_invoices",
                        principalColumn: "sales_invoice_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_collection_receipts_filpride_service_invoices_serv",
                        column: x => x.service_invoice_id,
                        principalTable: "filpride_service_invoices",
                        principalColumn: "service_invoice_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "filpride_credit_memos",
                columns: table => new
                {
                    credit_memo_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    credit_memo_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    sales_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    service_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    adjusted_price = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    credit_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    period = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    current_and_previous_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unearned_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
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
                    table.PrimaryKey("pk_filpride_credit_memos", x => x.credit_memo_id);
                    table.ForeignKey(
                        name: "fk_filpride_credit_memos_filpride_sales_invoices_sales_invoice",
                        column: x => x.sales_invoice_id,
                        principalTable: "filpride_sales_invoices",
                        principalColumn: "sales_invoice_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_credit_memos_filpride_service_invoices_service_inv",
                        column: x => x.service_invoice_id,
                        principalTable: "filpride_service_invoices",
                        principalColumn: "service_invoice_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "filpride_debit_memos",
                columns: table => new
                {
                    debit_memo_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sales_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    service_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    debit_memo_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    debit_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    adjusted_price = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    period = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    current_and_previous_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unearned_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
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
                    table.PrimaryKey("pk_filpride_debit_memos", x => x.debit_memo_id);
                    table.ForeignKey(
                        name: "fk_filpride_debit_memos_filpride_sales_invoices_sales_invoice_",
                        column: x => x.sales_invoice_id,
                        principalTable: "filpride_sales_invoices",
                        principalColumn: "sales_invoice_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_debit_memos_filpride_service_invoices_service_invo",
                        column: x => x.service_invoice_id,
                        principalTable: "filpride_service_invoices",
                        principalColumn: "service_invoice_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_app_settings_setting_key",
                table: "app_settings",
                column: "setting_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_role_claims_role_id",
                table: "AspNetRoleClaims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_claims_user_id",
                table: "AspNetUserClaims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_logins_user_id",
                table: "AspNetUserLogins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_roles_role_id",
                table: "AspNetUserRoles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bienes_placements_bank_id",
                table: "bienes_placements",
                column: "bank_id");

            migrationBuilder.CreateIndex(
                name: "ix_bienes_placements_company_id",
                table: "bienes_placements",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_bienes_placements_control_number",
                table: "bienes_placements",
                column: "control_number");

            migrationBuilder.CreateIndex(
                name: "ix_bienes_placements_rolled_from_id",
                table: "bienes_placements",
                column: "rolled_from_id");

            migrationBuilder.CreateIndex(
                name: "ix_bienes_placements_settlement_account_id",
                table: "bienes_placements",
                column: "settlement_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_bienes_placements_swapped_from_id",
                table: "bienes_placements",
                column: "swapped_from_id");

            migrationBuilder.CreateIndex(
                name: "ix_companies_company_code",
                table: "companies",
                column: "company_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_companies_company_name",
                table: "companies",
                column: "company_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_authority_to_loads_customer_order_slip_id",
                table: "filpride_authority_to_loads",
                column: "customer_order_slip_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_authority_to_loads_supplier_id",
                table: "filpride_authority_to_loads",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_book_atl_details_appointed_id",
                table: "filpride_book_atl_details",
                column: "appointed_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_book_atl_details_authority_to_load_id",
                table: "filpride_book_atl_details",
                column: "authority_to_load_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_book_atl_details_customer_order_slip_id",
                table: "filpride_book_atl_details",
                column: "customer_order_slip_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_chart_of_accounts_account_name",
                table: "filpride_chart_of_accounts",
                column: "account_name");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_chart_of_accounts_account_number",
                table: "filpride_chart_of_accounts",
                column: "account_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_chart_of_accounts_parent_account_id",
                table: "filpride_chart_of_accounts",
                column: "parent_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_check_voucher_details_bank_id",
                table: "filpride_check_voucher_details",
                column: "bank_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_check_voucher_details_check_voucher_header_id",
                table: "filpride_check_voucher_details",
                column: "check_voucher_header_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_check_voucher_details_company_id",
                table: "filpride_check_voucher_details",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_check_voucher_details_customer_id",
                table: "filpride_check_voucher_details",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_check_voucher_details_employee_id",
                table: "filpride_check_voucher_details",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_check_voucher_details_supplier_id",
                table: "filpride_check_voucher_details",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_check_voucher_headers_bank_id",
                table: "filpride_check_voucher_headers",
                column: "bank_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_check_voucher_headers_employee_id",
                table: "filpride_check_voucher_headers",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_check_voucher_headers_supplier_id",
                table: "filpride_check_voucher_headers",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_collection_receipts_bank_id",
                table: "filpride_collection_receipts",
                column: "bank_id");

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
                name: "ix_filpride_cos_appointed_suppliers_customer_order_slip_id",
                table: "filpride_cos_appointed_suppliers",
                column: "customer_order_slip_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_cos_appointed_suppliers_purchase_order_id",
                table: "filpride_cos_appointed_suppliers",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_cos_appointed_suppliers_supplier_id",
                table: "filpride_cos_appointed_suppliers",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_credit_memos_sales_invoice_id",
                table: "filpride_credit_memos",
                column: "sales_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_credit_memos_service_invoice_id",
                table: "filpride_credit_memos",
                column: "service_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_branches_customer_id",
                table: "filpride_customer_branches",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_order_slips_commissionee_id",
                table: "filpride_customer_order_slips",
                column: "commissionee_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_order_slips_customer_id",
                table: "filpride_customer_order_slips",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_order_slips_customer_order_slip_no",
                table: "filpride_customer_order_slips",
                column: "customer_order_slip_no");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_order_slips_date",
                table: "filpride_customer_order_slips",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_order_slips_hauler_id",
                table: "filpride_customer_order_slips",
                column: "hauler_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_order_slips_pick_up_point_id",
                table: "filpride_customer_order_slips",
                column: "pick_up_point_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_order_slips_product_id",
                table: "filpride_customer_order_slips",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_order_slips_purchase_order_id",
                table: "filpride_customer_order_slips",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_order_slips_supplier_id",
                table: "filpride_customer_order_slips",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customers_customer_code",
                table: "filpride_customers",
                column: "customer_code");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customers_customer_name",
                table: "filpride_customers",
                column: "customer_name");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_cv_trade_payments_check_voucher_id",
                table: "filpride_cv_trade_payments",
                column: "check_voucher_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_debit_memos_sales_invoice_id",
                table: "filpride_debit_memos",
                column: "sales_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_debit_memos_service_invoice_id",
                table: "filpride_debit_memos",
                column: "service_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_receipts_authority_to_load_id",
                table: "filpride_delivery_receipts",
                column: "authority_to_load_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_receipts_commissionee_id",
                table: "filpride_delivery_receipts",
                column: "commissionee_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_receipts_customer_id",
                table: "filpride_delivery_receipts",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_receipts_customer_order_slip_id",
                table: "filpride_delivery_receipts",
                column: "customer_order_slip_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_receipts_date",
                table: "filpride_delivery_receipts",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_receipts_delivery_receipt_no",
                table: "filpride_delivery_receipts",
                column: "delivery_receipt_no");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_receipts_hauler_id",
                table: "filpride_delivery_receipts",
                column: "hauler_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_receipts_purchase_order_id",
                table: "filpride_delivery_receipts",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_employees_employee_number",
                table: "filpride_employees",
                column: "employee_number");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_freights_pick_up_point_id",
                table: "filpride_freights",
                column: "pick_up_point_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_general_ledger_books_account_id",
                table: "filpride_general_ledger_books",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_general_ledger_books_bank_account_id",
                table: "filpride_general_ledger_books",
                column: "bank_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_general_ledger_books_company_id",
                table: "filpride_general_ledger_books",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_general_ledger_books_customer_id",
                table: "filpride_general_ledger_books",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_general_ledger_books_employee_id",
                table: "filpride_general_ledger_books",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_general_ledger_books_supplier_id",
                table: "filpride_general_ledger_books",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_inventories_po_id",
                table: "filpride_inventories",
                column: "po_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_inventories_product_id",
                table: "filpride_inventories",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_journal_voucher_details_journal_voucher_header_id",
                table: "filpride_journal_voucher_details",
                column: "journal_voucher_header_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_journal_voucher_headers_cv_id",
                table: "filpride_journal_voucher_headers",
                column: "cv_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_monthly_nibits_company",
                table: "filpride_monthly_nibits",
                column: "company");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_monthly_nibits_month",
                table: "filpride_monthly_nibits",
                column: "month");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_monthly_nibits_year",
                table: "filpride_monthly_nibits",
                column: "year");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_multiple_check_voucher_payments_check_voucher_head",
                table: "filpride_multiple_check_voucher_payments",
                column: "check_voucher_header_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_multiple_check_voucher_payments_check_voucher_head1",
                table: "filpride_multiple_check_voucher_payments",
                column: "check_voucher_header_payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_pick_up_points_company",
                table: "filpride_pick_up_points",
                column: "company");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_pick_up_points_supplier_id",
                table: "filpride_pick_up_points",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_po_actual_prices_purchase_order_id",
                table: "filpride_po_actual_prices",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_purchase_locked_records_queues_locked_date",
                table: "filpride_purchase_locked_records_queues",
                column: "locked_date");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_purchase_locked_records_queues_receiving_report_id",
                table: "filpride_purchase_locked_records_queues",
                column: "receiving_report_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_purchase_orders_customer_id",
                table: "filpride_purchase_orders",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_purchase_orders_pick_up_point_id",
                table: "filpride_purchase_orders",
                column: "pick_up_point_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_purchase_orders_product_id",
                table: "filpride_purchase_orders",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_purchase_orders_supplier_id",
                table: "filpride_purchase_orders",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_receiving_reports_delivery_receipt_id",
                table: "filpride_receiving_reports",
                column: "delivery_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_receiving_reports_po_id",
                table: "filpride_receiving_reports",
                column: "po_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_sales_invoices_customer_id",
                table: "filpride_sales_invoices",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_sales_invoices_customer_order_slip_id",
                table: "filpride_sales_invoices",
                column: "customer_order_slip_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_sales_invoices_delivery_receipt_id",
                table: "filpride_sales_invoices",
                column: "delivery_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_sales_invoices_product_id",
                table: "filpride_sales_invoices",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_sales_invoices_purchase_order_id",
                table: "filpride_sales_invoices",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_sales_locked_records_queues_delivery_receipt_id",
                table: "filpride_sales_locked_records_queues",
                column: "delivery_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_sales_locked_records_queues_locked_date",
                table: "filpride_sales_locked_records_queues",
                column: "locked_date");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_service_invoices_customer_id",
                table: "filpride_service_invoices",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_service_invoices_delivery_receipt_id",
                table: "filpride_service_invoices",
                column: "delivery_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_service_invoices_service_id",
                table: "filpride_service_invoices",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_suppliers_supplier_code",
                table: "filpride_suppliers",
                column: "supplier_code");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_suppliers_supplier_name",
                table: "filpride_suppliers",
                column: "supplier_name");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_billings_customer_id",
                table: "mmsi_billings",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_billings_mmsi_collection_id",
                table: "mmsi_billings",
                column: "mmsi_collection_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_billings_port_id",
                table: "mmsi_billings",
                column: "port_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_billings_principal_id",
                table: "mmsi_billings",
                column: "principal_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_billings_terminal_id",
                table: "mmsi_billings",
                column: "terminal_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_billings_vessel_id",
                table: "mmsi_billings",
                column: "vessel_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_collections_customer_id",
                table: "mmsi_collections",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_dispatch_tickets_customer_id",
                table: "mmsi_dispatch_tickets",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_dispatch_tickets_service_id",
                table: "mmsi_dispatch_tickets",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_dispatch_tickets_terminal_id",
                table: "mmsi_dispatch_tickets",
                column: "terminal_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_dispatch_tickets_tug_boat_id",
                table: "mmsi_dispatch_tickets",
                column: "tug_boat_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_dispatch_tickets_tug_master_id",
                table: "mmsi_dispatch_tickets",
                column: "tug_master_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_dispatch_tickets_vessel_id",
                table: "mmsi_dispatch_tickets",
                column: "vessel_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_principals_customer_id",
                table: "mmsi_principals",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_tariff_rates_customer_id",
                table: "mmsi_tariff_rates",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_tariff_rates_service_id",
                table: "mmsi_tariff_rates",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_tariff_rates_terminal_id",
                table: "mmsi_tariff_rates",
                column: "terminal_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_terminals_port_id",
                table: "mmsi_terminals",
                column: "port_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_tugboats_tugboat_owner_id",
                table: "mmsi_tugboats",
                column: "tugboat_owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_chart_of_accounts_account_name",
                table: "mobility_chart_of_accounts",
                column: "account_name");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_chart_of_accounts_account_number",
                table: "mobility_chart_of_accounts",
                column: "account_number",
                unique: true);

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
                name: "ix_mobility_collection_receipts_customer_id",
                table: "mobility_collection_receipts",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_collection_receipts_service_invoice_id",
                table: "mobility_collection_receipts",
                column: "service_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_credit_memos_service_invoice_id",
                table: "mobility_credit_memos",
                column: "service_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_customer_order_slips_customer_id",
                table: "mobility_customer_order_slips",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_customer_order_slips_product_id",
                table: "mobility_customer_order_slips",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_customer_order_slips_station_code",
                table: "mobility_customer_order_slips",
                column: "station_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_customer_purchase_orders_customer_id",
                table: "mobility_customer_purchase_orders",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_customer_purchase_orders_product_id",
                table: "mobility_customer_purchase_orders",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_customer_purchase_orders_station_code",
                table: "mobility_customer_purchase_orders",
                column: "station_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_customers_customer_id",
                table: "mobility_customers",
                column: "customer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mobility_cv_trade_payments_check_voucher_id",
                table: "mobility_cv_trade_payments",
                column: "check_voucher_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_debit_memos_service_invoice_id",
                table: "mobility_debit_memos",
                column: "service_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fms_calibrations_shift_record_id",
                table: "mobility_fms_calibrations",
                column: "shift_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fms_calibrations_station_code",
                table: "mobility_fms_calibrations",
                column: "station_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fms_cashier_shifts_shift_record_id",
                table: "mobility_fms_cashier_shifts",
                column: "shift_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fms_cashier_shifts_station_code",
                table: "mobility_fms_cashier_shifts",
                column: "station_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fms_fuel_sales_shift_record_id",
                table: "mobility_fms_fuel_sales",
                column: "shift_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fms_fuel_sales_station_code",
                table: "mobility_fms_fuel_sales",
                column: "station_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fms_lube_sales_shift_record_id",
                table: "mobility_fms_lube_sales",
                column: "shift_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fms_lube_sales_station_code",
                table: "mobility_fms_lube_sales",
                column: "station_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuel_deliveries_pagenumber",
                table: "mobility_fuel_deliveries",
                column: "pagenumber");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuel_deliveries_stncode",
                table: "mobility_fuel_deliveries",
                column: "stncode");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuel_purchase_fuel_purchase_no",
                table: "mobility_fuel_purchase",
                column: "fuel_purchase_no");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuel_purchase_product_code",
                table: "mobility_fuel_purchase",
                column: "product_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuel_purchase_station_code",
                table: "mobility_fuel_purchase",
                column: "station_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuels_inv_date",
                table: "mobility_fuels",
                column: "inv_date");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuels_item_code",
                table: "mobility_fuels",
                column: "item_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuels_particulars",
                table: "mobility_fuels",
                column: "particulars");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuels_shift",
                table: "mobility_fuels",
                column: "shift");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuels_x_oname",
                table: "mobility_fuels",
                column: "x_oname");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuels_x_pump",
                table: "mobility_fuels",
                column: "x_pump");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuels_x_sitecode",
                table: "mobility_fuels",
                column: "x_sitecode");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuels_x_ticket_id",
                table: "mobility_fuels",
                column: "x_ticket_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_general_ledgers_account_number",
                table: "mobility_general_ledgers",
                column: "account_number");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_general_ledgers_account_title",
                table: "mobility_general_ledgers",
                column: "account_title");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_general_ledgers_customer_code",
                table: "mobility_general_ledgers",
                column: "customer_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_general_ledgers_journal_reference",
                table: "mobility_general_ledgers",
                column: "journal_reference");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_general_ledgers_product_code",
                table: "mobility_general_ledgers",
                column: "product_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_general_ledgers_reference",
                table: "mobility_general_ledgers",
                column: "reference");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_general_ledgers_station_code",
                table: "mobility_general_ledgers",
                column: "station_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_general_ledgers_supplier_code",
                table: "mobility_general_ledgers",
                column: "supplier_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_general_ledgers_transaction_date",
                table: "mobility_general_ledgers",
                column: "transaction_date");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_inventories_product_code",
                table: "mobility_inventories",
                column: "product_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_inventories_station_code",
                table: "mobility_inventories",
                column: "station_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_inventories_transaction_no",
                table: "mobility_inventories",
                column: "transaction_no");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_journal_voucher_details_journal_voucher_header_id",
                table: "mobility_journal_voucher_details",
                column: "journal_voucher_header_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_journal_voucher_headers_cv_id",
                table: "mobility_journal_voucher_headers",
                column: "cv_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_lube_deliveries_pagenumber",
                table: "mobility_lube_deliveries",
                column: "pagenumber");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_lube_deliveries_stncode",
                table: "mobility_lube_deliveries",
                column: "stncode");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_lube_purchase_details_lube_purchase_header_id",
                table: "mobility_lube_purchase_details",
                column: "lube_purchase_header_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_lube_purchase_details_lube_purchase_header_no",
                table: "mobility_lube_purchase_details",
                column: "lube_purchase_header_no");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_lube_purchase_details_product_code",
                table: "mobility_lube_purchase_details",
                column: "product_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_lube_purchase_headers_lube_purchase_header_no",
                table: "mobility_lube_purchase_headers",
                column: "lube_purchase_header_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mobility_lube_purchase_headers_station_code",
                table: "mobility_lube_purchase_headers",
                column: "station_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_lubes_cashier",
                table: "mobility_lubes",
                column: "cashier");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_lubes_inv_date",
                table: "mobility_lubes",
                column: "inv_date");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_lubes_x_ticket_id",
                table: "mobility_lubes",
                column: "x_ticket_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_multiple_check_voucher_payments_check_voucher_head",
                table: "mobility_multiple_check_voucher_payments",
                column: "check_voucher_header_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_multiple_check_voucher_payments_check_voucher_head1",
                table: "mobility_multiple_check_voucher_payments",
                column: "check_voucher_header_payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_pick_up_points_supplier_id",
                table: "mobility_pick_up_points",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_po_sales_po_sales_no",
                table: "mobility_po_sales",
                column: "po_sales_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mobility_po_sales_raw_shiftrecid",
                table: "mobility_po_sales_raw",
                column: "shiftrecid");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_po_sales_raw_stncode",
                table: "mobility_po_sales_raw",
                column: "stncode");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_po_sales_raw_tripticket",
                table: "mobility_po_sales_raw",
                column: "tripticket");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_purchase_orders_pick_up_point_id",
                table: "mobility_purchase_orders",
                column: "pick_up_point_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_purchase_orders_product_id",
                table: "mobility_purchase_orders",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_purchase_orders_purchase_order_no",
                table: "mobility_purchase_orders",
                column: "purchase_order_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mobility_purchase_orders_station_code",
                table: "mobility_purchase_orders",
                column: "station_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_purchase_orders_supplier_id",
                table: "mobility_purchase_orders",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_receiving_reports_purchase_order_id",
                table: "mobility_receiving_reports",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_receiving_reports_receiving_report_no",
                table: "mobility_receiving_reports",
                column: "receiving_report_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mobility_receiving_reports_station_code",
                table: "mobility_receiving_reports",
                column: "station_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_safe_drops_inv_date",
                table: "mobility_safe_drops",
                column: "inv_date");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_safe_drops_x_oname",
                table: "mobility_safe_drops",
                column: "x_oname");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_safe_drops_x_ticket_id",
                table: "mobility_safe_drops",
                column: "x_ticket_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_sales_details_sales_header_id",
                table: "mobility_sales_details",
                column: "sales_header_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_sales_details_sales_no",
                table: "mobility_sales_details",
                column: "sales_no");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_sales_details_station_code",
                table: "mobility_sales_details",
                column: "station_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_sales_headers_cashier",
                table: "mobility_sales_headers",
                column: "cashier");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_sales_headers_date",
                table: "mobility_sales_headers",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_sales_headers_sales_no",
                table: "mobility_sales_headers",
                column: "sales_no");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_sales_headers_shift",
                table: "mobility_sales_headers",
                column: "shift");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_sales_headers_station_code",
                table: "mobility_sales_headers",
                column: "station_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_service_invoices_customer_id",
                table: "mobility_service_invoices",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_service_invoices_service_id",
                table: "mobility_service_invoices",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_service_invoices_station_code",
                table: "mobility_service_invoices",
                column: "station_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_station_employees_employee_number",
                table: "mobility_station_employees",
                column: "employee_number");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_stations_pos_code",
                table: "mobility_stations",
                column: "pos_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mobility_stations_station_code",
                table: "mobility_stations",
                column: "station_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mobility_stations_station_name",
                table: "mobility_stations",
                column: "station_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_product_code",
                table: "products",
                column: "product_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_product_name",
                table: "products",
                column: "product_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_notifications_notification_id",
                table: "user_notifications",
                column: "notification_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_notifications_user_id",
                table: "user_notifications",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_settings");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "bienes_placements");

            migrationBuilder.DropTable(
                name: "filpride_audit_trails");

            migrationBuilder.DropTable(
                name: "filpride_book_atl_details");

            migrationBuilder.DropTable(
                name: "filpride_cash_receipt_books");

            migrationBuilder.DropTable(
                name: "filpride_check_voucher_details");

            migrationBuilder.DropTable(
                name: "filpride_collection_receipts");

            migrationBuilder.DropTable(
                name: "filpride_credit_memos");

            migrationBuilder.DropTable(
                name: "filpride_customer_branches");

            migrationBuilder.DropTable(
                name: "filpride_cv_trade_payments");

            migrationBuilder.DropTable(
                name: "filpride_debit_memos");

            migrationBuilder.DropTable(
                name: "filpride_disbursement_books");

            migrationBuilder.DropTable(
                name: "filpride_freights");

            migrationBuilder.DropTable(
                name: "filpride_general_ledger_books");

            migrationBuilder.DropTable(
                name: "filpride_inventories");

            migrationBuilder.DropTable(
                name: "filpride_journal_books");

            migrationBuilder.DropTable(
                name: "filpride_journal_voucher_details");

            migrationBuilder.DropTable(
                name: "filpride_monthly_nibits");

            migrationBuilder.DropTable(
                name: "filpride_multiple_check_voucher_payments");

            migrationBuilder.DropTable(
                name: "filpride_offsettings");

            migrationBuilder.DropTable(
                name: "filpride_po_actual_prices");

            migrationBuilder.DropTable(
                name: "filpride_purchase_books");

            migrationBuilder.DropTable(
                name: "filpride_purchase_locked_records_queues");

            migrationBuilder.DropTable(
                name: "filpride_sales_books");

            migrationBuilder.DropTable(
                name: "filpride_sales_locked_records_queues");

            migrationBuilder.DropTable(
                name: "hub_connections");

            migrationBuilder.DropTable(
                name: "log_messages");

            migrationBuilder.DropTable(
                name: "mmsi_billings");

            migrationBuilder.DropTable(
                name: "mmsi_dispatch_tickets");

            migrationBuilder.DropTable(
                name: "mmsi_tariff_rates");

            migrationBuilder.DropTable(
                name: "mmsi_user_accesses");

            migrationBuilder.DropTable(
                name: "mobility_chart_of_accounts");

            migrationBuilder.DropTable(
                name: "mobility_check_voucher_details");

            migrationBuilder.DropTable(
                name: "mobility_collection_receipts");

            migrationBuilder.DropTable(
                name: "mobility_credit_memos");

            migrationBuilder.DropTable(
                name: "mobility_customer_order_slips");

            migrationBuilder.DropTable(
                name: "mobility_customer_purchase_orders");

            migrationBuilder.DropTable(
                name: "mobility_cv_trade_payments");

            migrationBuilder.DropTable(
                name: "mobility_debit_memos");

            migrationBuilder.DropTable(
                name: "mobility_fms_calibrations");

            migrationBuilder.DropTable(
                name: "mobility_fms_cashier_shifts");

            migrationBuilder.DropTable(
                name: "mobility_fms_deposits");

            migrationBuilder.DropTable(
                name: "mobility_fms_fuel_sales");

            migrationBuilder.DropTable(
                name: "mobility_fms_lube_sales");

            migrationBuilder.DropTable(
                name: "mobility_fms_po_sales");

            migrationBuilder.DropTable(
                name: "mobility_fuel_deliveries");

            migrationBuilder.DropTable(
                name: "mobility_fuel_purchase");

            migrationBuilder.DropTable(
                name: "mobility_fuels");

            migrationBuilder.DropTable(
                name: "mobility_general_ledgers");

            migrationBuilder.DropTable(
                name: "mobility_inventories");

            migrationBuilder.DropTable(
                name: "mobility_journal_voucher_details");

            migrationBuilder.DropTable(
                name: "mobility_log_reports");

            migrationBuilder.DropTable(
                name: "mobility_lube_deliveries");

            migrationBuilder.DropTable(
                name: "mobility_lube_purchase_details");

            migrationBuilder.DropTable(
                name: "mobility_lubes");

            migrationBuilder.DropTable(
                name: "mobility_multiple_check_voucher_payments");

            migrationBuilder.DropTable(
                name: "mobility_offlines");

            migrationBuilder.DropTable(
                name: "mobility_offsettings");

            migrationBuilder.DropTable(
                name: "mobility_po_sales");

            migrationBuilder.DropTable(
                name: "mobility_po_sales_raw");

            migrationBuilder.DropTable(
                name: "mobility_receiving_reports");

            migrationBuilder.DropTable(
                name: "mobility_safe_drops");

            migrationBuilder.DropTable(
                name: "mobility_sales_details");

            migrationBuilder.DropTable(
                name: "mobility_station_pumps");

            migrationBuilder.DropTable(
                name: "user_notifications");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "filpride_cos_appointed_suppliers");

            migrationBuilder.DropTable(
                name: "filpride_sales_invoices");

            migrationBuilder.DropTable(
                name: "filpride_service_invoices");

            migrationBuilder.DropTable(
                name: "companies");

            migrationBuilder.DropTable(
                name: "filpride_chart_of_accounts");

            migrationBuilder.DropTable(
                name: "filpride_journal_voucher_headers");

            migrationBuilder.DropTable(
                name: "filpride_receiving_reports");

            migrationBuilder.DropTable(
                name: "mmsi_collections");

            migrationBuilder.DropTable(
                name: "mmsi_principals");

            migrationBuilder.DropTable(
                name: "mmsi_tug_masters");

            migrationBuilder.DropTable(
                name: "mmsi_tugboats");

            migrationBuilder.DropTable(
                name: "mmsi_vessels");

            migrationBuilder.DropTable(
                name: "mmsi_services");

            migrationBuilder.DropTable(
                name: "mmsi_terminals");

            migrationBuilder.DropTable(
                name: "mobility_service_invoices");

            migrationBuilder.DropTable(
                name: "mobility_journal_voucher_headers");

            migrationBuilder.DropTable(
                name: "mobility_lube_purchase_headers");

            migrationBuilder.DropTable(
                name: "mobility_purchase_orders");

            migrationBuilder.DropTable(
                name: "mobility_sales_headers");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "filpride_services");

            migrationBuilder.DropTable(
                name: "filpride_check_voucher_headers");

            migrationBuilder.DropTable(
                name: "filpride_delivery_receipts");

            migrationBuilder.DropTable(
                name: "mmsi_tugboat_owners");

            migrationBuilder.DropTable(
                name: "mmsi_ports");

            migrationBuilder.DropTable(
                name: "mobility_customers");

            migrationBuilder.DropTable(
                name: "mobility_services");

            migrationBuilder.DropTable(
                name: "mobility_check_voucher_headers");

            migrationBuilder.DropTable(
                name: "mobility_pick_up_points");

            migrationBuilder.DropTable(
                name: "mobility_products");

            migrationBuilder.DropTable(
                name: "mobility_stations");

            migrationBuilder.DropTable(
                name: "filpride_bank_accounts");

            migrationBuilder.DropTable(
                name: "filpride_employees");

            migrationBuilder.DropTable(
                name: "filpride_authority_to_loads");

            migrationBuilder.DropTable(
                name: "mobility_bank_accounts");

            migrationBuilder.DropTable(
                name: "mobility_station_employees");

            migrationBuilder.DropTable(
                name: "mobility_suppliers");

            migrationBuilder.DropTable(
                name: "filpride_customer_order_slips");

            migrationBuilder.DropTable(
                name: "filpride_purchase_orders");

            migrationBuilder.DropTable(
                name: "filpride_customers");

            migrationBuilder.DropTable(
                name: "filpride_pick_up_points");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "filpride_suppliers");
        }
    }
}
