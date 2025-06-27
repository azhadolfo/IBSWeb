using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldsForApplicationOfVatInPurchaseOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "product_name",
                table: "filpride_purchase_orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "supplier_name",
                table: "filpride_purchase_orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tax_type",
                table: "filpride_purchase_orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "vat_type",
                table: "filpride_purchase_orders",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "product_name",
                table: "filpride_purchase_orders");

            migrationBuilder.DropColumn(
                name: "supplier_name",
                table: "filpride_purchase_orders");

            migrationBuilder.DropColumn(
                name: "tax_type",
                table: "filpride_purchase_orders");

            migrationBuilder.DropColumn(
                name: "vat_type",
                table: "filpride_purchase_orders");
        }
    }
}
