using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ChangePrimaryKeyInMobilityStationModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_stations",
                table: "mobility_stations");

            migrationBuilder.AddPrimaryKey(
                name: "pk_mobility_stations",
                table: "mobility_stations",
                column: "station_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mobility_customer_order_slips_mobility_stations_mobility_st",
                table: "mobility_customer_order_slips");

            migrationBuilder.DropForeignKey(
                name: "fk_mobility_customer_purchase_orders_mobility_stations_mobilit",
                table: "mobility_customer_purchase_orders");

            migrationBuilder.DropPrimaryKey(
                name: "pk_mobility_stations",
                table: "mobility_stations");

            migrationBuilder.DropIndex(
                name: "ix_mobility_customer_purchase_orders_mobility_station_station_",
                table: "mobility_customer_purchase_orders");

            migrationBuilder.DropIndex(
                name: "ix_mobility_customer_order_slips_mobility_station_station_code",
                table: "mobility_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "mobility_station_station_code",
                table: "mobility_customer_purchase_orders");

            migrationBuilder.DropColumn(
                name: "mobility_station_station_code",
                table: "mobility_customer_order_slips");

            migrationBuilder.AddColumn<int>(
                name: "mobility_station_station_id",
                table: "mobility_customer_purchase_orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "mobility_station_station_id",
                table: "mobility_customer_order_slips",
                type: "integer",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_mobility_stations",
                table: "mobility_stations",
                column: "station_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_customer_purchase_orders_mobility_station_station_",
                table: "mobility_customer_purchase_orders",
                column: "mobility_station_station_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_customer_order_slips_mobility_station_station_id",
                table: "mobility_customer_order_slips",
                column: "mobility_station_station_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mobility_customer_order_slips_mobility_stations_mobility_st",
                table: "mobility_customer_order_slips",
                column: "mobility_station_station_id",
                principalTable: "mobility_stations",
                principalColumn: "station_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mobility_customer_purchase_orders_mobility_stations_mobilit",
                table: "mobility_customer_purchase_orders",
                column: "mobility_station_station_id",
                principalTable: "mobility_stations",
                principalColumn: "station_id");
        }
    }
}
