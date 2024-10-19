using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierInTheFilprideCOS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "supplier_id",
                table: "filpride_customer_order_slips",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_order_slips_supplier_id",
                table: "filpride_customer_order_slips",
                column: "supplier_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_suppliers_supplier_id",
                table: "filpride_customer_order_slips",
                column: "supplier_id",
                principalTable: "filpride_suppliers",
                principalColumn: "supplier_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_suppliers_supplier_id",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropIndex(
                name: "ix_filpride_customer_order_slips_supplier_id",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "supplier_id",
                table: "filpride_customer_order_slips");
        }
    }
}
