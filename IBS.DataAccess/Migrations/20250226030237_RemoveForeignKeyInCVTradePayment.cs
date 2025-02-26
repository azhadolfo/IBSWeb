using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveForeignKeyInCVTradePayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_cv_trade_payments_filpride_delivery_receipts_docum",
                table: "filpride_cv_trade_payments");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_cv_trade_payments_filpride_receiving_reports_docum",
                table: "filpride_cv_trade_payments");

            migrationBuilder.DropIndex(
                name: "ix_filpride_cv_trade_payments_document_id",
                table: "filpride_cv_trade_payments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_filpride_cv_trade_payments_document_id",
                table: "filpride_cv_trade_payments",
                column: "document_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_cv_trade_payments_filpride_delivery_receipts_docum",
                table: "filpride_cv_trade_payments",
                column: "document_id",
                principalTable: "filpride_delivery_receipts",
                principalColumn: "delivery_receipt_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_cv_trade_payments_filpride_receiving_reports_docum",
                table: "filpride_cv_trade_payments",
                column: "document_id",
                principalTable: "filpride_receiving_reports",
                principalColumn: "receiving_report_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
