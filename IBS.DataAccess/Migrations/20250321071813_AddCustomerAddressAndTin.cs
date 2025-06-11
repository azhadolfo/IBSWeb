using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerAddressAndTin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "customer_address",
                table: "filpride_sales_invoices",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "customer_tin",
                table: "filpride_sales_invoices",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "customer_address",
                table: "filpride_delivery_receipts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "customer_tin",
                table: "filpride_delivery_receipts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "customer_address",
                table: "filpride_customer_order_slips",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "customer_tin",
                table: "filpride_customer_order_slips",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "customer_address",
                table: "filpride_sales_invoices");

            migrationBuilder.DropColumn(
                name: "customer_tin",
                table: "filpride_sales_invoices");

            migrationBuilder.DropColumn(
                name: "customer_address",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropColumn(
                name: "customer_tin",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropColumn(
                name: "customer_address",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "customer_tin",
                table: "filpride_customer_order_slips");
        }
    }
}
