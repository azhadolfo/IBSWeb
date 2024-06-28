using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemovePurchaseOrderForeignKeyInFilprideDeliveryReportsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_delivery_reports_filpride_purchase_orders_purchase",
                table: "filpride_delivery_reports");

            migrationBuilder.DropIndex(
                name: "ix_filpride_delivery_reports_purchase_order_id",
                table: "filpride_delivery_reports");

            migrationBuilder.DropColumn(
                name: "purchase_order_id",
                table: "filpride_delivery_reports");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "purchase_order_id",
                table: "filpride_delivery_reports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_reports_purchase_order_id",
                table: "filpride_delivery_reports",
                column: "purchase_order_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_delivery_reports_filpride_purchase_orders_purchase",
                table: "filpride_delivery_reports",
                column: "purchase_order_id",
                principalTable: "filpride_purchase_orders",
                principalColumn: "purchase_order_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
