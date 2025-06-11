using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveForeignKeyInMobilityPurchaseOrderModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mobility_customer_purchase_orders_mobility_stations_station",
                table: "mobility_customer_purchase_orders");

            migrationBuilder.DropIndex(
                name: "ix_mobility_customer_purchase_orders_station_id",
                table: "mobility_customer_purchase_orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mobility_customer_purchase_orders_mobility_stations_mobilit",
                table: "mobility_customer_purchase_orders");

            migrationBuilder.DropIndex(
                name: "ix_mobility_customer_purchase_orders_mobility_station_station_",
                table: "mobility_customer_purchase_orders");

            migrationBuilder.DropColumn(
                name: "mobility_station_station_id",
                table: "mobility_customer_purchase_orders");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_customer_purchase_orders_station_id",
                table: "mobility_customer_purchase_orders",
                column: "station_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mobility_customer_purchase_orders_mobility_stations_station",
                table: "mobility_customer_purchase_orders",
                column: "station_id",
                principalTable: "mobility_stations",
                principalColumn: "station_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
