using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchAmountAndBAFAmountInBillingModel : Migration
    {
        /// <summary>
        /// Applies schema changes to the mmsi_billings table: makes customer_id nullable, adds baf_amount and dispatch_amount columns, and updates the foreign key to filpride_customers.
        /// </summary>
        /// <remarks>
        /// - Drops the existing foreign key constraint on mmsi_billings.customer_id.
        /// - Alters mmsi_billings.customer_id to allow nulls.
        /// - Adds non-nullable numeric columns baf_amount and dispatch_amount with a default value of 0.
        /// - Re-creates the foreign key from mmsi_billings.customer_id to filpride_customers.customer_id (no explicit cascade behavior specified).
        /// </remarks>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_billings_filpride_customers_customer_id",
                table: "mmsi_billings");

            migrationBuilder.AlterColumn<int>(
                name: "customer_id",
                table: "mmsi_billings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<decimal>(
                name: "baf_amount",
                table: "mmsi_billings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "dispatch_amount",
                table: "mmsi_billings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_billings_filpride_customers_customer_id",
                table: "mmsi_billings",
                column: "customer_id",
                principalTable: "filpride_customers",
                principalColumn: "customer_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_billings_filpride_customers_customer_id",
                table: "mmsi_billings");

            migrationBuilder.DropColumn(
                name: "baf_amount",
                table: "mmsi_billings");

            migrationBuilder.DropColumn(
                name: "dispatch_amount",
                table: "mmsi_billings");

            migrationBuilder.AlterColumn<int>(
                name: "customer_id",
                table: "mmsi_billings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_billings_filpride_customers_customer_id",
                table: "mmsi_billings",
                column: "customer_id",
                principalTable: "filpride_customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}