using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAddressAndTinForAdditionalTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "customer_address",
                table: "filpride_service_invoices",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "customer_tin",
                table: "filpride_service_invoices",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "supplier_address",
                table: "filpride_purchase_orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "supplier_tin",
                table: "filpride_purchase_orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "address",
                table: "filpride_check_voucher_headers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tin",
                table: "filpride_check_voucher_headers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "customer_address",
                table: "filpride_service_invoices");

            migrationBuilder.DropColumn(
                name: "customer_tin",
                table: "filpride_service_invoices");

            migrationBuilder.DropColumn(
                name: "supplier_address",
                table: "filpride_purchase_orders");

            migrationBuilder.DropColumn(
                name: "supplier_tin",
                table: "filpride_purchase_orders");

            migrationBuilder.DropColumn(
                name: "address",
                table: "filpride_check_voucher_headers");

            migrationBuilder.DropColumn(
                name: "tin",
                table: "filpride_check_voucher_headers");
        }
    }
}
