using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerOrderSlipIdToFilprideATL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_authority_to_loads_filpride_delivery_receipts_deli",
                table: "filpride_authority_to_loads");

            migrationBuilder.AlterColumn<int>(
                name: "delivery_receipt_id",
                table: "filpride_authority_to_loads",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "customer_order_slip_id",
                table: "filpride_authority_to_loads",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_authority_to_loads_customer_order_slip_id",
                table: "filpride_authority_to_loads",
                column: "customer_order_slip_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_authority_to_loads_filpride_customer_order_slips_c",
                table: "filpride_authority_to_loads",
                column: "customer_order_slip_id",
                principalTable: "filpride_customer_order_slips",
                principalColumn: "customer_order_slip_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_authority_to_loads_filpride_delivery_receipts_deli",
                table: "filpride_authority_to_loads",
                column: "delivery_receipt_id",
                principalTable: "filpride_delivery_receipts",
                principalColumn: "delivery_receipt_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_authority_to_loads_filpride_customer_order_slips_c",
                table: "filpride_authority_to_loads");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_authority_to_loads_filpride_delivery_receipts_deli",
                table: "filpride_authority_to_loads");

            migrationBuilder.DropIndex(
                name: "ix_filpride_authority_to_loads_customer_order_slip_id",
                table: "filpride_authority_to_loads");

            migrationBuilder.DropColumn(
                name: "customer_order_slip_id",
                table: "filpride_authority_to_loads");

            migrationBuilder.AlterColumn<int>(
                name: "delivery_receipt_id",
                table: "filpride_authority_to_loads",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_authority_to_loads_filpride_delivery_receipts_deli",
                table: "filpride_authority_to_loads",
                column: "delivery_receipt_id",
                principalTable: "filpride_delivery_receipts",
                principalColumn: "delivery_receipt_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
