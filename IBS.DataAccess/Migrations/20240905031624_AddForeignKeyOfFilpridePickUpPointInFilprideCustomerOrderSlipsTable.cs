using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeyOfFilpridePickUpPointInFilprideCustomerOrderSlipsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pick_up_point",
                table: "filpride_customer_order_slips");

            migrationBuilder.AddColumn<int>(
                name: "pick_up_point_id",
                table: "filpride_customer_order_slips",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_order_slips_pick_up_point_id",
                table: "filpride_customer_order_slips",
                column: "pick_up_point_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_pick_up_points_pick_",
                table: "filpride_customer_order_slips",
                column: "pick_up_point_id",
                principalTable: "filpride_pick_up_points",
                principalColumn: "pick_up_point_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_pick_up_points_pick_",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropIndex(
                name: "ix_filpride_customer_order_slips_pick_up_point_id",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "pick_up_point_id",
                table: "filpride_customer_order_slips");

            migrationBuilder.AddColumn<string>(
                name: "pick_up_point",
                table: "filpride_customer_order_slips",
                type: "varchar(20)",
                nullable: true);
        }
    }
}
