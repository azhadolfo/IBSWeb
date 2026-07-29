using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPartySnapshotToLockedPeriodAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "customer_id",
                table: "locked_period_adjustments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customer_name",
                table: "locked_period_adjustments",
                type: "varchar(200)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "supplier_id",
                table: "locked_period_adjustments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supplier_name",
                table: "locked_period_adjustments",
                type: "varchar(200)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "locked_period_adjustments");

            migrationBuilder.DropColumn(
                name: "customer_name",
                table: "locked_period_adjustments");

            migrationBuilder.DropColumn(
                name: "supplier_id",
                table: "locked_period_adjustments");

            migrationBuilder.DropColumn(
                name: "supplier_name",
                table: "locked_period_adjustments");
        }
    }
}
