using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCustomerForeignKeyInFilprideDeliveryReportsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_delivery_reports_customers_customer_id",
                table: "filpride_delivery_reports");

            migrationBuilder.DropIndex(
                name: "ix_filpride_delivery_reports_customer_id",
                table: "filpride_delivery_reports");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "filpride_delivery_reports");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "customer_id",
                table: "filpride_delivery_reports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_reports_customer_id",
                table: "filpride_delivery_reports",
                column: "customer_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_delivery_reports_customers_customer_id",
                table: "filpride_delivery_reports",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
