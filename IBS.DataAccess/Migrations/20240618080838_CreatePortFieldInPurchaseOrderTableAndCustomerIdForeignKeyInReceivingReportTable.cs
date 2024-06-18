using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CreatePortFieldInPurchaseOrderTableAndCustomerIdForeignKeyInReceivingReportTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "customer_id",
                table: "filpride_receiving_reports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "port",
                table: "filpride_purchase_orders",
                type: "varchar(100)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_receiving_reports_customer_id",
                table: "filpride_receiving_reports",
                column: "customer_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_receiving_reports_customers_customer_id",
                table: "filpride_receiving_reports",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_receiving_reports_customers_customer_id",
                table: "filpride_receiving_reports");

            migrationBuilder.DropIndex(
                name: "ix_filpride_receiving_reports_customer_id",
                table: "filpride_receiving_reports");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "filpride_receiving_reports");

            migrationBuilder.DropColumn(
                name: "port",
                table: "filpride_purchase_orders");
        }
    }
}
