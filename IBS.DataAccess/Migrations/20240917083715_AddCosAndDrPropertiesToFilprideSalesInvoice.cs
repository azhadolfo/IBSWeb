using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCosAndDrPropertiesToFilprideSalesInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "customer_order_slip_id",
                table: "filpride_sales_invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "delivery_receipt_id",
                table: "filpride_sales_invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_sales_invoices_customer_order_slip_id",
                table: "filpride_sales_invoices",
                column: "customer_order_slip_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_sales_invoices_delivery_receipt_id",
                table: "filpride_sales_invoices",
                column: "delivery_receipt_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_sales_invoices_filpride_customer_order_slips_custo",
                table: "filpride_sales_invoices",
                column: "customer_order_slip_id",
                principalTable: "filpride_customer_order_slips",
                principalColumn: "customer_order_slip_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_sales_invoices_filpride_delivery_receipts_delivery",
                table: "filpride_sales_invoices",
                column: "delivery_receipt_id",
                principalTable: "filpride_delivery_receipts",
                principalColumn: "delivery_receipt_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_sales_invoices_filpride_customer_order_slips_custo",
                table: "filpride_sales_invoices");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_sales_invoices_filpride_delivery_receipts_delivery",
                table: "filpride_sales_invoices");

            migrationBuilder.DropIndex(
                name: "ix_filpride_sales_invoices_customer_order_slip_id",
                table: "filpride_sales_invoices");

            migrationBuilder.DropIndex(
                name: "ix_filpride_sales_invoices_delivery_receipt_id",
                table: "filpride_sales_invoices");

            migrationBuilder.DropColumn(
                name: "customer_order_slip_id",
                table: "filpride_sales_invoices");

            migrationBuilder.DropColumn(
                name: "delivery_receipt_id",
                table: "filpride_sales_invoices");
        }
    }
}
