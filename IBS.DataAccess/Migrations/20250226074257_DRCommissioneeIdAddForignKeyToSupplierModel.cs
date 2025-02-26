using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class DRCommissioneeIdAddForignKeyToSupplierModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "commissionee_id",
                table: "filpride_delivery_receipts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_receipts_commissionee_id",
                table: "filpride_delivery_receipts",
                column: "commissionee_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_delivery_receipts_filpride_suppliers_commissionee_",
                table: "filpride_delivery_receipts",
                column: "commissionee_id",
                principalTable: "filpride_suppliers",
                principalColumn: "supplier_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_delivery_receipts_filpride_suppliers_commissionee_",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropIndex(
                name: "ix_filpride_delivery_receipts_commissionee_id",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropColumn(
                name: "commissionee_id",
                table: "filpride_delivery_receipts");
        }
    }
}
