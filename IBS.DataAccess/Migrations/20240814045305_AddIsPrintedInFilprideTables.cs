using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPrintedInFilprideTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_printed",
                table: "receiving_reports",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_printed",
                table: "purchase_orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_printed",
                table: "filpride_service_invoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_printed",
                table: "filpride_sales_invoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_printed",
                table: "filpride_journal_voucher_headers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_printed",
                table: "filpride_debit_memos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_printed",
                table: "filpride_credit_memos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_printed",
                table: "filpride_collection_receipts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_printed",
                table: "filpride_check_voucher_headers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_printed",
                table: "receiving_reports");

            migrationBuilder.DropColumn(
                name: "is_printed",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "is_printed",
                table: "filpride_service_invoices");

            migrationBuilder.DropColumn(
                name: "is_printed",
                table: "filpride_sales_invoices");

            migrationBuilder.DropColumn(
                name: "is_printed",
                table: "filpride_journal_voucher_headers");

            migrationBuilder.DropColumn(
                name: "is_printed",
                table: "filpride_debit_memos");

            migrationBuilder.DropColumn(
                name: "is_printed",
                table: "filpride_credit_memos");

            migrationBuilder.DropColumn(
                name: "is_printed",
                table: "filpride_collection_receipts");

            migrationBuilder.DropColumn(
                name: "is_printed",
                table: "filpride_check_voucher_headers");
        }
    }
}
