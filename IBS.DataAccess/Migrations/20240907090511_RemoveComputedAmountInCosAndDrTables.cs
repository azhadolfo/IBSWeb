using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveComputedAmountInCosAndDrTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "net_of_vat_amount",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropColumn(
                name: "vat_amount",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropColumn(
                name: "vat",
                table: "filpride_customer_order_slips");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "net_of_vat_amount",
                table: "filpride_delivery_receipts",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "vat_amount",
                table: "filpride_delivery_receipts",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "vat",
                table: "filpride_customer_order_slips",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
