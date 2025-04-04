using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerToDispatchTicketModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "customer_id",
                table: "mmsi_dispatch_tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_dispatch_tickets_customer_id",
                table: "mmsi_dispatch_tickets",
                column: "customer_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_customers_customer_id",
                table: "mmsi_dispatch_tickets",
                column: "customer_id",
                principalTable: "mmsi_customers",
                principalColumn: "mmsi_customer_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_customers_customer_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropIndex(
                name: "ix_mmsi_dispatch_tickets_customer_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "mmsi_dispatch_tickets");
        }
    }
}
