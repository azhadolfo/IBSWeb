using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSubPoPropertiesInPurchaseOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_sub_po",
                table: "filpride_purchase_orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "sub_po_series",
                table: "filpride_purchase_orders",
                type: "varchar(20)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_sub_po",
                table: "filpride_purchase_orders");

            migrationBuilder.DropColumn(
                name: "sub_po_series",
                table: "filpride_purchase_orders");
        }
    }
}
