using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchAmountAndBAFAmountInBillingModel : Migration
    {
        /// <inheritdoc />
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
