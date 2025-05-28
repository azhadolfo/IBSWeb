using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveForeignKeyInMobilityCOSModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mobility_customer_order_slips_mobility_stations_station_id",
                table: "mobility_customer_order_slips");

            migrationBuilder.DropIndex(
                name: "ix_mobility_customer_order_slips_station_id",
                table: "mobility_customer_order_slips");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mobility_customer_order_slips_mobility_stations_mobility_st",
                table: "mobility_customer_order_slips");

            migrationBuilder.DropIndex(
                name: "ix_mobility_customer_order_slips_mobility_station_station_id",
                table: "mobility_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "mobility_station_station_id",
                table: "mobility_customer_order_slips");

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
    }
}
