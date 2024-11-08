using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldNameTypeToAllDocumentsForImplementingDocumentedAndUndocumented : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "filpride_service_invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "filpride_receiving_reports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "filpride_purchase_orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "filpride_journal_voucher_headers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "filpride_debit_memos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "filpride_credit_memos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "filpride_collection_receipts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "filpride_check_voucher_headers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "type",
                table: "filpride_service_invoices");

            migrationBuilder.DropColumn(
                name: "type",
                table: "filpride_receiving_reports");

            migrationBuilder.DropColumn(
                name: "type",
                table: "filpride_purchase_orders");

            migrationBuilder.DropColumn(
                name: "type",
                table: "filpride_journal_voucher_headers");

            migrationBuilder.DropColumn(
                name: "type",
                table: "filpride_debit_memos");

            migrationBuilder.DropColumn(
                name: "type",
                table: "filpride_credit_memos");

            migrationBuilder.DropColumn(
                name: "type",
                table: "filpride_collection_receipts");

            migrationBuilder.DropColumn(
                name: "type",
                table: "filpride_check_voucher_headers");
        }
    }
}
