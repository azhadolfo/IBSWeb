using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeyOfProductOnTheFilprideCOSTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_order_slips_product_id",
                table: "filpride_customer_order_slips",
                column: "product_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_customer_order_slips_products_product_id",
                table: "filpride_customer_order_slips",
                column: "product_id",
                principalTable: "products",
                principalColumn: "product_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_customer_order_slips_products_product_id",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropIndex(
                name: "ix_filpride_customer_order_slips_product_id",
                table: "filpride_customer_order_slips");
        }
    }
}
