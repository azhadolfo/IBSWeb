using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameTheUntriggeredVolumeToUntriggeredQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "un_triggered_volume",
                table: "filpride_purchase_orders",
                newName: "un_triggered_quantity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "un_triggered_quantity",
                table: "filpride_purchase_orders",
                newName: "un_triggered_volume");
        }
    }
}
