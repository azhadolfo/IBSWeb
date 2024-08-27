using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerInSubPoDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "customer_id",
                table: "filpride_purchase_orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_purchase_orders_customer_id",
                table: "filpride_purchase_orders",
                column: "customer_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_purchase_orders_filpride_customers_customer_id",
                table: "filpride_purchase_orders",
                column: "customer_id",
                principalTable: "filpride_customers",
                principalColumn: "customer_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_purchase_orders_filpride_customers_customer_id",
                table: "filpride_purchase_orders");

            migrationBuilder.DropIndex(
                name: "ix_filpride_purchase_orders_customer_id",
                table: "filpride_purchase_orders");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "filpride_purchase_orders");
        }
    }
}
