using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AdditionOfBillingNumberInDispatchTicketModel : Migration
    {
        /// <summary>
        /// Modifies the mmsi_dispatch_tickets schema to add a billing number, convert billing_id to integer, and establish an index and foreign key to mmsi_billings.
        /// </summary>
        /// <remarks>
        /// Alters mmsi_dispatch_tickets.billing_id to type integer, adds a nullable text column billing_number, creates index ix_mmsi_dispatch_tickets_billing_id on billing_id, and adds foreign key fk_mmsi_dispatch_tickets_mmsi_billings_billing_id referencing mmsi_billings(mmsi_billing_id).
        /// </remarks>
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

        /// <summary>
        /// Reverts the migration by removing the foreign key and index on mmsi_dispatch_tickets.billing_id, dropping the billing_number column, and changing mmsi_dispatch_tickets.billing_id back to a nullable text column.
        /// </summary>
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