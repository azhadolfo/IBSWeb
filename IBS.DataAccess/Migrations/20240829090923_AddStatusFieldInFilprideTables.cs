using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusFieldInFilprideTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.RenameColumn(
                name: "status",
                table: "filpride_service_invoices",
                newName: "payment_status");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "filpride_sales_invoices",
                newName: "payment_status");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "filpride_receiving_reports",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "filpride_purchase_orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "filpride_journal_voucher_headers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "filpride_debit_memos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "filpride_credit_memos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "filpride_collection_receipts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "filpride_check_voucher_headers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_journal_voucher_headers_filpride_check_voucher_hea",
                table: "filpride_journal_voucher_headers");

            migrationBuilder.DropColumn(
                name: "status",
                table: "filpride_receiving_reports");

            migrationBuilder.DropColumn(
                name: "status",
                table: "filpride_purchase_orders");

            migrationBuilder.DropColumn(
                name: "status",
                table: "filpride_journal_voucher_headers");

            migrationBuilder.DropColumn(
                name: "status",
                table: "filpride_debit_memos");

            migrationBuilder.DropColumn(
                name: "status",
                table: "filpride_credit_memos");

            migrationBuilder.DropColumn(
                name: "status",
                table: "filpride_collection_receipts");

            migrationBuilder.DropColumn(
                name: "status",
                table: "filpride_check_voucher_headers");

            migrationBuilder.RenameColumn(
                name: "payment_status",
                table: "filpride_service_invoices",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "payment_status",
                table: "filpride_sales_invoices",
                newName: "status");

            migrationBuilder.AlterColumn<int>(
                name: "cv_id",
                table: "filpride_journal_voucher_headers",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_journal_voucher_headers_filpride_check_voucher_hea",
                table: "filpride_journal_voucher_headers",
                column: "cv_id",
                principalTable: "filpride_check_voucher_headers",
                principalColumn: "check_voucher_header_id");
        }
    }
}
