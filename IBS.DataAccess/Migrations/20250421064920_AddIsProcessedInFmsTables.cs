using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddIsProcessedInFmsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_processed",
                table: "mobility_fms_lube_sales",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_processed",
                table: "mobility_fms_fuel_sales",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_processed",
                table: "mobility_fms_cashier_shifts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_processed",
                table: "mobility_fms_calibrations",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_processed",
                table: "mobility_fms_lube_sales");

            migrationBuilder.DropColumn(
                name: "is_processed",
                table: "mobility_fms_fuel_sales");

            migrationBuilder.DropColumn(
                name: "is_processed",
                table: "mobility_fms_cashier_shifts");

            migrationBuilder.DropColumn(
                name: "is_processed",
                table: "mobility_fms_calibrations");
        }
    }
}
