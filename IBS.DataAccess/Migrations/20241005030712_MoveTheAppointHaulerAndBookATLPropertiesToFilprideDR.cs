using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class MoveTheAppointHaulerAndBookATLPropertiesToFilprideDR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_authority_to_loads_filpride_customer_order_slips_c",
                table: "filpride_authority_to_loads");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_suppliers_hauler_id",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropIndex(
                name: "ix_filpride_customer_order_slips_hauler_id",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "authority_to_load_no",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "driver",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "hauler_id",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "plate_no",
                table: "filpride_customer_order_slips");

            migrationBuilder.RenameColumn(
                name: "customer_order_slip_id",
                table: "filpride_authority_to_loads",
                newName: "delivery_receipt_id");

            migrationBuilder.RenameIndex(
                name: "ix_filpride_authority_to_loads_customer_order_slip_id",
                table: "filpride_authority_to_loads",
                newName: "ix_filpride_authority_to_loads_delivery_receipt_id");

            migrationBuilder.AddColumn<string>(
                name: "authority_to_load_no",
                table: "filpride_delivery_receipts",
                type: "varchar(20)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "driver",
                table: "filpride_delivery_receipts",
                type: "varchar(200)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "freight",
                table: "filpride_delivery_receipts",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "hauler_id",
                table: "filpride_delivery_receipts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "plate_no",
                table: "filpride_delivery_receipts",
                type: "varchar(200)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_receipts_hauler_id",
                table: "filpride_delivery_receipts",
                column: "hauler_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_authority_to_loads_filpride_delivery_receipts_deli",
                table: "filpride_authority_to_loads",
                column: "delivery_receipt_id",
                principalTable: "filpride_delivery_receipts",
                principalColumn: "delivery_receipt_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_delivery_receipts_filpride_suppliers_hauler_id",
                table: "filpride_delivery_receipts",
                column: "hauler_id",
                principalTable: "filpride_suppliers",
                principalColumn: "supplier_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_authority_to_loads_filpride_delivery_receipts_deli",
                table: "filpride_authority_to_loads");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_delivery_receipts_filpride_suppliers_hauler_id",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropIndex(
                name: "ix_filpride_delivery_receipts_hauler_id",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropColumn(
                name: "authority_to_load_no",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropColumn(
                name: "driver",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropColumn(
                name: "freight",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropColumn(
                name: "hauler_id",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropColumn(
                name: "plate_no",
                table: "filpride_delivery_receipts");

            migrationBuilder.RenameColumn(
                name: "delivery_receipt_id",
                table: "filpride_authority_to_loads",
                newName: "customer_order_slip_id");

            migrationBuilder.RenameIndex(
                name: "ix_filpride_authority_to_loads_delivery_receipt_id",
                table: "filpride_authority_to_loads",
                newName: "ix_filpride_authority_to_loads_customer_order_slip_id");

            migrationBuilder.AddColumn<string>(
                name: "authority_to_load_no",
                table: "filpride_customer_order_slips",
                type: "varchar(20)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "driver",
                table: "filpride_customer_order_slips",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "hauler_id",
                table: "filpride_customer_order_slips",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "plate_no",
                table: "filpride_customer_order_slips",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_order_slips_hauler_id",
                table: "filpride_customer_order_slips",
                column: "hauler_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_authority_to_loads_filpride_customer_order_slips_c",
                table: "filpride_authority_to_loads",
                column: "customer_order_slip_id",
                principalTable: "filpride_customer_order_slips",
                principalColumn: "customer_order_slip_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_suppliers_hauler_id",
                table: "filpride_customer_order_slips",
                column: "hauler_id",
                principalTable: "filpride_suppliers",
                principalColumn: "supplier_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
