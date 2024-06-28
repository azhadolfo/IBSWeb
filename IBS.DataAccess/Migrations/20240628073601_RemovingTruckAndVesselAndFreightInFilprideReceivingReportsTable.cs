using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemovingTruckAndVesselAndFreightInFilprideReceivingReportsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "freight",
                table: "filpride_receiving_reports");

            migrationBuilder.DropColumn(
                name: "truck_or_vessels",
                table: "filpride_receiving_reports");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "freight",
                table: "filpride_receiving_reports",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "truck_or_vessels",
                table: "filpride_receiving_reports",
                type: "varchar(100)",
                nullable: false,
                defaultValue: "");
        }
    }
}
