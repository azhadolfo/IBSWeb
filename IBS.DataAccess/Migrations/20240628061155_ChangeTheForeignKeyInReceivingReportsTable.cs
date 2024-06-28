using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTheForeignKeyInReceivingReportsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_receiving_reports_filpride_purchase_orders_purchas",
                table: "filpride_receiving_reports");

            migrationBuilder.RenameColumn(
                name: "purchase_order_id",
                table: "filpride_receiving_reports",
                newName: "delivery_receipt_id");

            migrationBuilder.RenameIndex(
                name: "ix_filpride_receiving_reports_purchase_order_id",
                table: "filpride_receiving_reports",
                newName: "ix_filpride_receiving_reports_delivery_receipt_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_receiving_reports_filpride_delivery_receipts_deliv",
                table: "filpride_receiving_reports",
                column: "delivery_receipt_id",
                principalTable: "filpride_delivery_receipts",
                principalColumn: "delivery_receipt_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_receiving_reports_filpride_delivery_receipts_deliv",
                table: "filpride_receiving_reports");

            migrationBuilder.RenameColumn(
                name: "delivery_receipt_id",
                table: "filpride_receiving_reports",
                newName: "purchase_order_id");

            migrationBuilder.RenameIndex(
                name: "ix_filpride_receiving_reports_delivery_receipt_id",
                table: "filpride_receiving_reports",
                newName: "ix_filpride_receiving_reports_purchase_order_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_receiving_reports_filpride_purchase_orders_purchas",
                table: "filpride_receiving_reports",
                column: "purchase_order_id",
                principalTable: "filpride_purchase_orders",
                principalColumn: "purchase_order_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
