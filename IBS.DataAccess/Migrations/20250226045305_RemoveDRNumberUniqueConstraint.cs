using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDRNumberUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_filpride_delivery_receipts_delivery_receipt_no",
                table: "filpride_delivery_receipts");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_receipts_delivery_receipt_no",
                table: "filpride_delivery_receipts",
                column: "delivery_receipt_no");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_filpride_delivery_receipts_delivery_receipt_no",
                table: "filpride_delivery_receipts");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_receipts_delivery_receipt_no",
                table: "filpride_delivery_receipts",
                column: "delivery_receipt_no",
                unique: true);
        }
    }
}
