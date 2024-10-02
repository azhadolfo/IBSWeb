using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CreateForeignKeyOfMasterFileToGeneralLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "bank_account_id",
                table: "filpride_general_ledger_books",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "customer_id",
                table: "filpride_general_ledger_books",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "supplier_id",
                table: "filpride_general_ledger_books",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_general_ledger_books_bank_account_id",
                table: "filpride_general_ledger_books",
                column: "bank_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_general_ledger_books_customer_id",
                table: "filpride_general_ledger_books",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_general_ledger_books_supplier_id",
                table: "filpride_general_ledger_books",
                column: "supplier_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_general_ledger_books_filpride_bank_accounts_bank_a",
                table: "filpride_general_ledger_books",
                column: "bank_account_id",
                principalTable: "filpride_bank_accounts",
                principalColumn: "bank_account_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_general_ledger_books_filpride_customers_customer_id",
                table: "filpride_general_ledger_books",
                column: "customer_id",
                principalTable: "filpride_customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_general_ledger_books_filpride_suppliers_supplier_id",
                table: "filpride_general_ledger_books",
                column: "supplier_id",
                principalTable: "filpride_suppliers",
                principalColumn: "supplier_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_general_ledger_books_filpride_bank_accounts_bank_a",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_general_ledger_books_filpride_customers_customer_id",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_general_ledger_books_filpride_suppliers_supplier_id",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropIndex(
                name: "ix_filpride_general_ledger_books_bank_account_id",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropIndex(
                name: "ix_filpride_general_ledger_books_customer_id",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropIndex(
                name: "ix_filpride_general_ledger_books_supplier_id",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropColumn(
                name: "bank_account_id",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropColumn(
                name: "supplier_id",
                table: "filpride_general_ledger_books");
        }
    }
}
