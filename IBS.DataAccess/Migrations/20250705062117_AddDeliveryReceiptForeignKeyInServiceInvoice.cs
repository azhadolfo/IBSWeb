using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryReceiptForeignKeyInServiceInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "delivery_receipt_id",
                table: "filpride_service_invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_service_invoices_delivery_receipt_id",
                table: "filpride_service_invoices",
                column: "delivery_receipt_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_service_invoices_filpride_delivery_receipts_delive",
                table: "filpride_service_invoices",
                column: "delivery_receipt_id",
                principalTable: "filpride_delivery_receipts",
                principalColumn: "delivery_receipt_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_service_invoices_filpride_delivery_receipts_delive",
                table: "filpride_service_invoices");

            migrationBuilder.DropIndex(
                name: "ix_filpride_service_invoices_delivery_receipt_id",
                table: "filpride_service_invoices");

            migrationBuilder.DropColumn(
                name: "delivery_receipt_id",
                table: "filpride_service_invoices");
        }
    }
}
