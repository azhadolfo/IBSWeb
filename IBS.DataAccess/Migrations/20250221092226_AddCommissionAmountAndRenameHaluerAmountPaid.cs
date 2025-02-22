using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCommissionAmountAndRenameHaluerAmountPaid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "hauler_amount_paid",
                table: "filpride_delivery_receipts",
                newName: "freight_amount_paid");

            migrationBuilder.AddColumn<decimal>(
                name: "commission_amount",
                table: "filpride_delivery_receipts",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "commission_amount",
                table: "filpride_delivery_receipts");

            migrationBuilder.RenameColumn(
                name: "freight_amount_paid",
                table: "filpride_delivery_receipts",
                newName: "hauler_amount_paid");
        }
    }
}
