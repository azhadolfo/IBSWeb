using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnDispatchAndBAFDiscountInTariffRateModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "baf_discount",
                table: "mmsi_tariff_rates",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "dispatch_discount",
                table: "mmsi_tariff_rates",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "baf_discount",
                table: "mmsi_tariff_rates");

            migrationBuilder.DropColumn(
                name: "dispatch_discount",
                table: "mmsi_tariff_rates");
        }
    }
}
