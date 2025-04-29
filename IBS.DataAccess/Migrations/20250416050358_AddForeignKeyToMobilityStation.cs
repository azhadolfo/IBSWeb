using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeyToMobilityStation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_mobility_customer_purchase_orders_station_code",
                table: "mobility_customer_purchase_orders",
                column: "station_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_customer_order_slips_station_code",
                table: "mobility_customer_order_slips",
                column: "station_code");

            migrationBuilder.AddForeignKey(
                name: "fk_mobility_customer_order_slips_mobility_stations_station_code",
                table: "mobility_customer_order_slips",
                column: "station_code",
                principalTable: "mobility_stations",
                principalColumn: "station_code",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mobility_customer_purchase_orders_mobility_stations_station",
                table: "mobility_customer_purchase_orders",
                column: "station_code",
                principalTable: "mobility_stations",
                principalColumn: "station_code",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mobility_customer_order_slips_mobility_stations_station_code",
                table: "mobility_customer_order_slips");

            migrationBuilder.DropForeignKey(
                name: "fk_mobility_customer_purchase_orders_mobility_stations_station",
                table: "mobility_customer_purchase_orders");

            migrationBuilder.DropIndex(
                name: "ix_mobility_customer_purchase_orders_station_code",
                table: "mobility_customer_purchase_orders");

            migrationBuilder.DropIndex(
                name: "ix_mobility_customer_order_slips_station_code",
                table: "mobility_customer_order_slips");

            migrationBuilder.AlterColumn<string>(
                name: "station_code",
                table: "mobility_customer_purchase_orders",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(5)");

            migrationBuilder.AddColumn<string>(
                name: "mobility_station_station_code",
                table: "mobility_customer_purchase_orders",
                type: "varchar(5)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mobility_station_station_code",
                table: "mobility_customer_order_slips",
                type: "varchar(5)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_mobility_customer_purchase_orders_mobility_station_station_",
                table: "mobility_customer_purchase_orders",
                column: "mobility_station_station_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_customer_order_slips_mobility_station_station_code",
                table: "mobility_customer_order_slips",
                column: "mobility_station_station_code");

            migrationBuilder.AddForeignKey(
                name: "fk_mobility_customer_order_slips_mobility_stations_mobility_st",
                table: "mobility_customer_order_slips",
                column: "mobility_station_station_code",
                principalTable: "mobility_stations",
                principalColumn: "station_code");

            migrationBuilder.AddForeignKey(
                name: "fk_mobility_customer_purchase_orders_mobility_stations_mobilit",
                table: "mobility_customer_purchase_orders",
                column: "mobility_station_station_code",
                principalTable: "mobility_stations",
                principalColumn: "station_code");
        }
    }
}
