using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeyInMobilityPurchaseOrderModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "fk_mobility_purchase_orders_mobility_stations_station_code",
                table: "mobility_purchase_orders",
                column: "station_code",
                principalTable: "mobility_stations",
                principalColumn: "station_code",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mobility_purchase_orders_mobility_stations_station_code",
                table: "mobility_purchase_orders");
        }
    }
}
