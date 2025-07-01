using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddFiledsToStoreForeignValuesInGLBookModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bank_account_name",
                table: "filpride_general_ledger_books",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company_name",
                table: "filpride_general_ledger_books",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "customer_name",
                table: "filpride_general_ledger_books",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "employee_name",
                table: "filpride_general_ledger_books",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "supplier_name",
                table: "filpride_general_ledger_books",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bank_account_name",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropColumn(
                name: "company_name",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropColumn(
                name: "customer_name",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropColumn(
                name: "employee_name",
                table: "filpride_general_ledger_books");

            migrationBuilder.DropColumn(
                name: "supplier_name",
                table: "filpride_general_ledger_books");
        }
    }
}
