using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameFieldInPOSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PurchaseOrderTime",
                table: "POSales",
                newName: "POSalesTime");

            migrationBuilder.RenameColumn(
                name: "PurchaseOrderNo",
                table: "POSales",
                newName: "POSalesNo");

            migrationBuilder.RenameColumn(
                name: "PurchaseOrderDate",
                table: "POSales",
                newName: "POSalesDate");

            migrationBuilder.RenameColumn(
                name: "PurchaseOrderId",
                table: "POSales",
                newName: "POSalesId");

            migrationBuilder.RenameIndex(
                name: "IX_POSales_PurchaseOrderNo",
                table: "POSales",
                newName: "IX_POSales_POSalesNo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "POSalesTime",
                table: "POSales",
                newName: "PurchaseOrderTime");

            migrationBuilder.RenameColumn(
                name: "POSalesNo",
                table: "POSales",
                newName: "PurchaseOrderNo");

            migrationBuilder.RenameColumn(
                name: "POSalesDate",
                table: "POSales",
                newName: "PurchaseOrderDate");

            migrationBuilder.RenameColumn(
                name: "POSalesId",
                table: "POSales",
                newName: "PurchaseOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_POSales_POSalesNo",
                table: "POSales",
                newName: "IX_POSales_PurchaseOrderNo");
        }
    }
}
