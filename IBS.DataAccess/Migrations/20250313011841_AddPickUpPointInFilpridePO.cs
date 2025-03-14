using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPickUpPointInFilpridePO : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "pick_up_point_id",
                table: "filpride_purchase_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_purchase_orders_pick_up_point_id",
                table: "filpride_purchase_orders",
                column: "pick_up_point_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_purchase_orders_filpride_pick_up_points_pick_up_po",
                table: "filpride_purchase_orders",
                column: "pick_up_point_id",
                principalTable: "filpride_pick_up_points",
                principalColumn: "pick_up_point_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_purchase_orders_filpride_pick_up_points_pick_up_po",
                table: "filpride_purchase_orders");

            migrationBuilder.DropIndex(
                name: "ix_filpride_purchase_orders_pick_up_point_id",
                table: "filpride_purchase_orders");

            migrationBuilder.DropColumn(
                name: "pick_up_point_id",
                table: "filpride_purchase_orders");
        }
    }
}
