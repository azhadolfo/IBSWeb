using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeyInCosAppointSuppliers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "fk_filpride_cos_appointed_suppliers_filpride_customer_order_sl",
                table: "filpride_cos_appointed_suppliers",
                column: "customer_order_slip_id",
                principalTable: "filpride_customer_order_slips",
                principalColumn: "customer_order_slip_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_cos_appointed_suppliers_filpride_customer_order_sl",
                table: "filpride_cos_appointed_suppliers");
        }
    }
}
