using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTheHaulerAndMoveToSupplier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_haulers_hauler_id",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_delivery_receipts_filpride_haulers_hauler_id",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropIndex(
                name: "ix_filpride_haulers_hauler_code",
                table: "filpride_haulers");

            migrationBuilder.DropIndex(
                name: "ix_filpride_haulers_hauler_name",
                table: "filpride_haulers");

            migrationBuilder.DropIndex(
                name: "ix_filpride_delivery_receipts_hauler_id",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropColumn(
                name: "hauler_id",
                table: "filpride_delivery_receipts");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_suppliers_hauler_id",
                table: "filpride_customer_order_slips",
                column: "hauler_id",
                principalTable: "filpride_suppliers",
                principalColumn: "supplier_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_suppliers_hauler_id",
                table: "filpride_customer_order_slips");

            migrationBuilder.AddColumn<int>(
                name: "hauler_id",
                table: "filpride_delivery_receipts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_haulers_hauler_code",
                table: "filpride_haulers",
                column: "hauler_code");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_haulers_hauler_name",
                table: "filpride_haulers",
                column: "hauler_name");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_receipts_hauler_id",
                table: "filpride_delivery_receipts",
                column: "hauler_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_haulers_hauler_id",
                table: "filpride_customer_order_slips",
                column: "hauler_id",
                principalTable: "filpride_haulers",
                principalColumn: "hauler_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_delivery_receipts_filpride_haulers_hauler_id",
                table: "filpride_delivery_receipts",
                column: "hauler_id",
                principalTable: "filpride_haulers",
                principalColumn: "hauler_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
