using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameIsHaulerPaidIntoIsFeightPaidInDR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "is_hauler_paid",
                table: "filpride_delivery_receipts",
                newName: "is_freight_paid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "is_freight_paid",
                table: "filpride_delivery_receipts",
                newName: "is_hauler_paid");
        }
    }
}
