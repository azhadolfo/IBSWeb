using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignkeyForMobilityStationCodeInMobilityCOSmodel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "station_id",
                table: "mobility_customer_order_slips",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_mobility_customer_order_slips_station_id",
                table: "mobility_customer_order_slips",
                column: "station_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mobility_customer_order_slips_mobility_stations_station_id",
                table: "mobility_customer_order_slips",
                column: "station_id",
                principalTable: "mobility_stations",
                principalColumn: "station_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mobility_customer_order_slips_mobility_stations_station_id",
                table: "mobility_customer_order_slips");

            migrationBuilder.DropIndex(
                name: "ix_mobility_customer_order_slips_station_id",
                table: "mobility_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "station_id",
                table: "mobility_customer_order_slips");
        }
    }
}
