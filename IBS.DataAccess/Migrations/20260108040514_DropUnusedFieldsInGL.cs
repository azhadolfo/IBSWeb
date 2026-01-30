using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class DropUnusedFieldsInGL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_general_ledger_books_companies_company_id",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_general_ledger_books_filpride_bank_accounts_bank_a",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_general_ledger_books_filpride_customers_customer_id",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_general_ledger_books_filpride_employees_employee_id",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_general_ledger_books_filpride_suppliers_supplier_id",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropIndex(
                name: "ix_filpride_general_ledger_books_bank_account_id",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropIndex(
                name: "ix_filpride_general_ledger_books_company_id",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropIndex(
                name: "ix_filpride_general_ledger_books_customer_id",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropIndex(
                name: "ix_filpride_general_ledger_books_employee_id",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropIndex(
                name: "ix_filpride_general_ledger_books_supplier_id",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropColumn(
                name: "bank_account_id",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropColumn(
                name: "bank_account_name",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropColumn(
                name: "company_id",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropColumn(
                name: "company_name",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropColumn(
                name: "customer_name",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropColumn(
                name: "employee_id",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropColumn(
                name: "employee_name",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropColumn(
                name: "supplier_id",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropColumn(
                name: "supplier_name",
                table: "filpride_general_ledger_books");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
