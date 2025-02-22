using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddNeededFieldsForImplementingCreateHaulerAndCommissionInCV : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "commission_amount_paid",
                table: "filpride_delivery_receipts",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "commission_rate",
                table: "filpride_delivery_receipts",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "commissionee_id",
                table: "filpride_delivery_receipts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "freight_amount",
                table: "filpride_delivery_receipts",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "hauler_amount_paid",
                table: "filpride_delivery_receipts",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "is_commission_paid",
                table: "filpride_delivery_receipts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_hauler_paid",
                table: "filpride_delivery_receipts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "commission_amount_paid",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropColumn(
                name: "commission_rate",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropColumn(
                name: "commissionee_id",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropColumn(
                name: "freight_amount",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropColumn(
                name: "hauler_amount_paid",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropColumn(
                name: "is_commission_paid",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropColumn(
                name: "is_hauler_paid",
                table: "filpride_delivery_receipts");
        }
    }
}
