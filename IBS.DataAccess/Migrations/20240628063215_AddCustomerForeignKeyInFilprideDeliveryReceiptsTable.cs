using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerForeignKeyInFilprideDeliveryReceiptsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "customer_id",
                table: "filpride_delivery_receipts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_receipts_customer_id",
                table: "filpride_delivery_receipts",
                column: "customer_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_delivery_receipts_customers_customer_id",
                table: "filpride_delivery_receipts",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_delivery_receipts_customers_customer_id",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropIndex(
                name: "ix_filpride_delivery_receipts_customer_id",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "filpride_delivery_receipts");
        }
    }
}
