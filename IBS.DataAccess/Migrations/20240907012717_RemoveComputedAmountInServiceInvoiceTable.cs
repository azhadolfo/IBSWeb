using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveComputedAmountInServiceInvoiceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "net_amount",
                table: "filpride_service_invoices");

            migrationBuilder.DropColumn(
                name: "vat_amount",
                table: "filpride_service_invoices");

            migrationBuilder.DropColumn(
                name: "withholding_tax_amount",
                table: "filpride_service_invoices");

            migrationBuilder.DropColumn(
                name: "withholding_vat_amount",
                table: "filpride_service_invoices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "net_amount",
                table: "filpride_service_invoices",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "vat_amount",
                table: "filpride_service_invoices",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "withholding_tax_amount",
                table: "filpride_service_invoices",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "withholding_vat_amount",
                table: "filpride_service_invoices",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
