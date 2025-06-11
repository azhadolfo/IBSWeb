using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ModifyForeignKeyOfServiceRequestCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_customers_customer_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_filpride_customers_customer_id",
                table: "mmsi_dispatch_tickets",
                column: "customer_id",
                principalTable: "filpride_customers",
                principalColumn: "customer_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_filpride_customers_customer_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_customers_customer_id",
                table: "mmsi_dispatch_tickets",
                column: "customer_id",
                principalTable: "mmsi_customers",
                principalColumn: "mmsi_customer_id");
        }
    }
}
