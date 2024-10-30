using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddHaulerAndBookATlToFilprideCOS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "fk_filpride_customer_order_slips_filpride_suppliers_hauler_id",
                table: "filpride_customer_order_slips",
                column: "hauler_id",
                principalTable: "filpride_suppliers",
                principalColumn: "supplier_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
