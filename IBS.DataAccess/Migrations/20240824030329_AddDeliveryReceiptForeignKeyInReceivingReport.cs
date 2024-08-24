using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryReceiptForeignKeyInReceivingReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "delivery_receipt_id",
                table: "filpride_receiving_reports",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_receiving_reports_delivery_receipt_id",
                table: "filpride_receiving_reports",
                column: "delivery_receipt_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_receiving_reports_filpride_delivery_receipts_deliv",
                table: "filpride_receiving_reports",
                column: "delivery_receipt_id",
                principalTable: "filpride_delivery_receipts",
                principalColumn: "delivery_receipt_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_receiving_reports_filpride_delivery_receipts_deliv",
                table: "filpride_receiving_reports");

            migrationBuilder.DropIndex(
                name: "ix_filpride_receiving_reports_delivery_receipt_id",
                table: "filpride_receiving_reports");

            migrationBuilder.DropColumn(
                name: "delivery_receipt_id",
                table: "filpride_receiving_reports");
        }
    }
}
