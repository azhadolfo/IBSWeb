using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderForeignKeyInFilprideCustomerOrderSlipsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "po_no",
                table: "filpride_customer_order_slips",
                newName: "customer_po_no");

            migrationBuilder.AddColumn<int>(
                name: "purchase_order_id",
                table: "filpride_customer_order_slips",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_order_slips_purchase_order_id",
                table: "filpride_customer_order_slips",
                column: "purchase_order_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_purchase_orders_purc",
                table: "filpride_customer_order_slips",
                column: "purchase_order_id",
                principalTable: "filpride_purchase_orders",
                principalColumn: "purchase_order_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_purchase_orders_purc",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropIndex(
                name: "ix_filpride_customer_order_slips_purchase_order_id",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "purchase_order_id",
                table: "filpride_customer_order_slips");

            migrationBuilder.RenameColumn(
                name: "customer_po_no",
                table: "filpride_customer_order_slips",
                newName: "po_no");
        }
    }
}
