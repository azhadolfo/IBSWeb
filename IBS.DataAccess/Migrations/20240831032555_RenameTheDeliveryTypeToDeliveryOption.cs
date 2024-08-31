using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameTheDeliveryTypeToDeliveryOption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "delivery_type",
                table: "filpride_delivery_receipts",
                newName: "delivery_option");

            migrationBuilder.RenameColumn(
                name: "delivery_type",
                table: "filpride_customer_order_slips",
                newName: "delivery_option");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "delivery_option",
                table: "filpride_delivery_receipts",
                newName: "delivery_type");

            migrationBuilder.RenameColumn(
                name: "delivery_option",
                table: "filpride_customer_order_slips",
                newName: "delivery_type");
        }
    }
}
