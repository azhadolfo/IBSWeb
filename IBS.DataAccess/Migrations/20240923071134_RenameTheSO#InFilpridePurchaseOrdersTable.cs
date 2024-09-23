using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameTheSOInFilpridePurchaseOrdersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "sales_order_no",
                table: "filpride_purchase_orders",
                newName: "supplier_sales_order_no");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "supplier_sales_order_no",
                table: "filpride_purchase_orders",
                newName: "sales_order_no");
        }
    }
}
