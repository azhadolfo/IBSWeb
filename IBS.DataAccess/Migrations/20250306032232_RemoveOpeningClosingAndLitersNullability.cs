using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOpeningClosingAndLitersNullability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_filpride_delivery_receipts_delivery_receipt_no",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropIndex(
                name: "ix_filpride_customer_order_slips_customer_order_slip_no",
                table: "filpride_customer_order_slips");

            migrationBuilder.AlterColumn<decimal>(
                name: "opening",
                table: "mobility_fuels",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "liters",
                table: "mobility_fuels",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "closing",
                table: "mobility_fuels",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_receipts_delivery_receipt_no",
                table: "filpride_delivery_receipts",
                column: "delivery_receipt_no");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_order_slips_customer_order_slip_no",
                table: "filpride_customer_order_slips",
                column: "customer_order_slip_no");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_filpride_delivery_receipts_delivery_receipt_no",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropIndex(
                name: "ix_filpride_customer_order_slips_customer_order_slip_no",
                table: "filpride_customer_order_slips");

            migrationBuilder.AlterColumn<decimal>(
                name: "opening",
                table: "mobility_fuels",
                type: "numeric",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "liters",
                table: "mobility_fuels",
                type: "numeric",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "closing",
                table: "mobility_fuels",
                type: "numeric",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_receipts_delivery_receipt_no",
                table: "filpride_delivery_receipts",
                column: "delivery_receipt_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_order_slips_customer_order_slip_no",
                table: "filpride_customer_order_slips",
                column: "customer_order_slip_no",
                unique: true);
        }
    }
}
