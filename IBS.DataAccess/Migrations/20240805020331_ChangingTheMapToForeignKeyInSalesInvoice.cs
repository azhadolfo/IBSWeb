using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ChangingTheMapToForeignKeyInSalesInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_sales_invoices_filpride_purchase_orders_purchase_o",
                table: "filpride_sales_invoices");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_sales_invoices_purchase_orders_purchase_order_id",
                table: "filpride_sales_invoices",
                column: "purchase_order_id",
                principalTable: "purchase_orders",
                principalColumn: "purchase_order_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_sales_invoices_purchase_orders_purchase_order_id",
                table: "filpride_sales_invoices");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_sales_invoices_filpride_purchase_orders_purchase_o",
                table: "filpride_sales_invoices",
                column: "purchase_order_id",
                principalTable: "filpride_purchase_orders",
                principalColumn: "purchase_order_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
