using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddHaulerForeignKeyOnTheFilprideCOSTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_suppliers_commission",
                table: "filpride_customer_order_slips");

            migrationBuilder.AddColumn<int>(
                name: "hauler_id",
                table: "filpride_customer_order_slips",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_order_slips_hauler_id",
                table: "filpride_customer_order_slips",
                column: "hauler_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_haulers_hauler_id",
                table: "filpride_customer_order_slips",
                column: "hauler_id",
                principalTable: "filpride_haulers",
                principalColumn: "hauler_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_suppliers_commission",
                table: "filpride_customer_order_slips",
                column: "commissioner_id",
                principalTable: "filpride_suppliers",
                principalColumn: "supplier_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_haulers_hauler_id",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_suppliers_commission",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropIndex(
                name: "ix_filpride_customer_order_slips_hauler_id",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "hauler_id",
                table: "filpride_customer_order_slips");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_suppliers_commission",
                table: "filpride_customer_order_slips",
                column: "commissioner_id",
                principalTable: "filpride_suppliers",
                principalColumn: "supplier_id");
        }
    }
}
