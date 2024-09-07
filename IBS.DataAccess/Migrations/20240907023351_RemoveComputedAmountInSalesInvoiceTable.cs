using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveComputedAmountInSalesInvoiceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "net_discount",
                table: "filpride_sales_invoices");

            migrationBuilder.DropColumn(
                name: "vat_amount",
                table: "filpride_sales_invoices");

            migrationBuilder.DropColumn(
                name: "vat_exempt",
                table: "filpride_sales_invoices");

            migrationBuilder.DropColumn(
                name: "vatable_sales",
                table: "filpride_sales_invoices");

            migrationBuilder.DropColumn(
                name: "with_holding_tax_amount",
                table: "filpride_sales_invoices");

            migrationBuilder.DropColumn(
                name: "with_holding_vat_amount",
                table: "filpride_sales_invoices");

            migrationBuilder.DropColumn(
                name: "zero_rated",
                table: "filpride_sales_invoices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "net_discount",
                table: "filpride_sales_invoices",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "vat_amount",
                table: "filpride_sales_invoices",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "vat_exempt",
                table: "filpride_sales_invoices",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "vatable_sales",
                table: "filpride_sales_invoices",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "with_holding_tax_amount",
                table: "filpride_sales_invoices",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "with_holding_vat_amount",
                table: "filpride_sales_invoices",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "zero_rated",
                table: "filpride_sales_invoices",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
