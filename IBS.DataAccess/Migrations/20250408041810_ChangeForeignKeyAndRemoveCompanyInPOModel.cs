using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ChangeForeignKeyAndRemoveCompanyInPOModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mobility_purchase_orders_filpride_pick_up_points_pick_up_po",
                table: "mobility_purchase_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_mobility_purchase_orders_products_product_id",
                table: "mobility_purchase_orders");

            migrationBuilder.DropColumn(
                name: "company",
                table: "mobility_purchase_orders");

            migrationBuilder.AddForeignKey(
                name: "fk_mobility_purchase_orders_mobility_pick_up_points_pick_up_po",
                table: "mobility_purchase_orders",
                column: "pick_up_point_id",
                principalTable: "mobility_pick_up_points",
                principalColumn: "pick_up_point_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mobility_purchase_orders_mobility_products_product_id",
                table: "mobility_purchase_orders",
                column: "product_id",
                principalTable: "mobility_products",
                principalColumn: "product_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mobility_purchase_orders_mobility_pick_up_points_pick_up_po",
                table: "mobility_purchase_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_mobility_purchase_orders_mobility_products_product_id",
                table: "mobility_purchase_orders");

            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "mobility_purchase_orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "fk_mobility_purchase_orders_filpride_pick_up_points_pick_up_po",
                table: "mobility_purchase_orders",
                column: "pick_up_point_id",
                principalTable: "filpride_pick_up_points",
                principalColumn: "pick_up_point_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mobility_purchase_orders_products_product_id",
                table: "mobility_purchase_orders",
                column: "product_id",
                principalTable: "products",
                principalColumn: "product_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
