using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ChangeForeignKeyCallingInCustomerModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mobility_customer_order_slips_filpride_customers_customer_id",
                table: "mobility_customer_order_slips");

            migrationBuilder.AddForeignKey(
                name: "fk_mobility_customer_order_slips_mobility_customers_customer_id",
                table: "mobility_customer_order_slips",
                column: "customer_id",
                principalTable: "mobility_customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mobility_customer_order_slips_mobility_customers_customer_id",
                table: "mobility_customer_order_slips");

            migrationBuilder.AddForeignKey(
                name: "fk_mobility_customer_order_slips_filpride_customers_customer_id",
                table: "mobility_customer_order_slips",
                column: "customer_id",
                principalTable: "filpride_customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
