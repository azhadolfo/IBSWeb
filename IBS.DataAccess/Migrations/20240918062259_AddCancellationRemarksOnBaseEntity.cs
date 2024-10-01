using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCancellationRemarksOnBaseEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "mobility_sales_headers",
                type: "varchar(255)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "mobility_safe_drops",
                type: "varchar(255)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "mobility_receiving_reports",
                type: "varchar(255)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "mobility_purchase_orders",
                type: "varchar(255)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "mobility_po_sales",
                type: "varchar(255)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "mobility_lubes",
                type: "varchar(255)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "mobility_lube_purchase_headers",
                type: "varchar(255)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "mobility_fuels",
                type: "varchar(255)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "mobility_fuel_purchase",
                type: "varchar(255)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "filpride_service_invoices",
                type: "varchar(255)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "filpride_sales_invoices",
                type: "varchar(255)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "filpride_receiving_reports",
                type: "varchar(255)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "filpride_purchase_orders",
                type: "varchar(255)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "filpride_journal_voucher_headers",
                type: "varchar(255)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "filpride_delivery_receipts",
                type: "varchar(255)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "filpride_debit_memos",
                type: "varchar(255)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "filpride_credit_memos",
                type: "varchar(255)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "filpride_collection_receipts",
                type: "varchar(255)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "filpride_check_voucher_headers",
                type: "varchar(255)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "mobility_sales_headers");

            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "mobility_safe_drops");

            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "mobility_receiving_reports");

            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "mobility_purchase_orders");

            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "mobility_po_sales");

            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "mobility_lubes");

            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "mobility_lube_purchase_headers");

            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "mobility_fuels");

            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "mobility_fuel_purchase");

            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "filpride_service_invoices");

            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "filpride_sales_invoices");

            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "filpride_receiving_reports");

            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "filpride_purchase_orders");

            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "filpride_journal_voucher_headers");

            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "filpride_debit_memos");

            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "filpride_credit_memos");

            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "filpride_collection_receipts");

            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "filpride_check_voucher_headers");
        }
    }
}
