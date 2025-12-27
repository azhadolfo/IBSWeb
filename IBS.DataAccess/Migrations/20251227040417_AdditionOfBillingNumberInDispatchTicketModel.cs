using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AdditionOfBillingNumberInDispatchTicketModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"ALTER TABLE mmsi_dispatch_tickets
                  ALTER COLUMN billing_id
                  TYPE integer
                  USING billing_id::integer;");

            migrationBuilder.AddColumn<string>(
                name: "billing_number",
                table: "mmsi_dispatch_tickets",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_dispatch_tickets_billing_id",
                table: "mmsi_dispatch_tickets",
                column: "billing_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_billings_billing_id",
                table: "mmsi_dispatch_tickets",
                column: "billing_id",
                principalTable: "mmsi_billings",
                principalColumn: "mmsi_billing_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_billings_billing_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropIndex(
                name: "ix_mmsi_dispatch_tickets_billing_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropColumn(
                name: "billing_number",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.AlterColumn<string>(
                name: "billing_id",
                table: "mmsi_dispatch_tickets",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
