using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyFieldInEachFilprideTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_bank_accounts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "receiving_reports",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "purchase_orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "haulers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_suppliers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_services",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_service_invoices",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_sales_invoices",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_sales_books",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_purchase_books",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_journal_voucher_headers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_journal_books",
                type: "text",
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
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_disbursement_books",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_debit_memos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_customers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_credit_memos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_collection_receipts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_check_voucher_headers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "filpride_cash_receipt_books",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "company",
                table: "receiving_reports");

            migrationBuilder.DropColumn(
                name: "company",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "company",
                table: "haulers");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_suppliers");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_services");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_service_invoices");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_sales_invoices");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_sales_books");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_purchase_books");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_journal_voucher_headers");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_journal_books");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_inventories");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_disbursement_books");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_debit_memos");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_customers");

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
                table: "filpride_cash_receipt_books");

            migrationBuilder.DropColumn(
                name: "company",
                table: "filpride_bank_accounts");

        }
    }
}
