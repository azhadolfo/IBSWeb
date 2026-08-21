using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class MoveTheStoringOfCwtAndCwvToDR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cw_vat_percent",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "cwt_percent",
                table: "filpride_customer_order_slips");

            migrationBuilder.AddColumn<decimal>(
                name: "cwt_percent",
                table: "filpride_delivery_receipts",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "cwv_percent",
                table: "filpride_delivery_receipts",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cwt_percent",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropColumn(
                name: "cwv_percent",
                table: "filpride_delivery_receipts");

            migrationBuilder.AddColumn<decimal>(
                name: "cw_vat_percent",
                table: "filpride_customer_order_slips",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "cwt_percent",
                table: "filpride_customer_order_slips",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
