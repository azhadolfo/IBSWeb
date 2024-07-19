using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameTheStationToMobilityStation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename the table from "stations" to "mobility_stations"
            migrationBuilder.RenameTable(
                name: "stations",
                newName: "mobility_stations");

            // Update indexes if necessary
            migrationBuilder.RenameIndex(
                name: "ix_stations_pos_code",
                table: "mobility_stations",
                newName: "ix_mobility_stations_pos_code");

            migrationBuilder.RenameIndex(
                name: "ix_stations_station_code",
                table: "mobility_stations",
                newName: "ix_mobility_stations_station_code");

            migrationBuilder.RenameIndex(
                name: "ix_stations_station_name",
                table: "mobility_stations",
                newName: "ix_mobility_stations_station_name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rename the table back from "mobility_stations" to "stations"
            migrationBuilder.RenameTable(
                name: "mobility_stations",
                newName: "stations");

            // Update indexes if necessary
            migrationBuilder.RenameIndex(
                name: "ix_mobility_stations_pos_code",
                table: "stations",
                newName: "ix_stations_pos_code");

            migrationBuilder.RenameIndex(
                name: "ix_mobility_stations_station_code",
                table: "stations",
                newName: "ix_stations_station_code");

            migrationBuilder.RenameIndex(
                name: "ix_mobility_stations_station_name",
                table: "stations",
                newName: "ix_stations_station_name");
        }
    }
}
