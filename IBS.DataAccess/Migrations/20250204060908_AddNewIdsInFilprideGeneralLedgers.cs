using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddNewIdsInFilprideGeneralLedgers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "account_id",
                table: "filpride_general_ledger_books",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "employee_id",
                table: "filpride_general_ledger_books",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_general_ledger_books_account_id",
                table: "filpride_general_ledger_books",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_general_ledger_books_employee_id",
                table: "filpride_general_ledger_books",
                column: "employee_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_general_ledger_books_filpride_chart_of_accounts_ac",
                table: "filpride_general_ledger_books",
                column: "account_id",
                principalTable: "filpride_chart_of_accounts",
                principalColumn: "account_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_general_ledger_books_filpride_employees_employee_id",
                table: "filpride_general_ledger_books",
                column: "employee_id",
                principalTable: "filpride_employees",
                principalColumn: "employee_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_general_ledger_books_filpride_chart_of_accounts_ac",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_general_ledger_books_filpride_employees_employee_id",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropIndex(
                name: "ix_filpride_general_ledger_books_account_id",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropIndex(
                name: "ix_filpride_general_ledger_books_employee_id",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropColumn(
                name: "account_id",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropColumn(
                name: "employee_id",
                table: "filpride_general_ledger_books");
        }
    }
}
