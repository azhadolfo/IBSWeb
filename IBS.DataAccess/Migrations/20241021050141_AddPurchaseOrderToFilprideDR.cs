using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderToFilprideDR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "purchase_order_id",
                table: "filpride_delivery_receipts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_receipts_purchase_order_id",
                table: "filpride_delivery_receipts",
                column: "purchase_order_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_delivery_receipts_filpride_purchase_orders_purchas",
                table: "filpride_delivery_receipts",
                column: "purchase_order_id",
                principalTable: "filpride_purchase_orders",
                principalColumn: "purchase_order_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_delivery_receipts_filpride_purchase_orders_purchas",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropIndex(
                name: "ix_filpride_delivery_receipts_purchase_order_id",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropColumn(
                name: "purchase_order_id",
                table: "filpride_delivery_receipts");
        }
    }
}
