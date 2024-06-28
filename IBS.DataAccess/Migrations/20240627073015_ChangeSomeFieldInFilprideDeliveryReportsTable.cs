using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ChangeSomeFieldInFilprideDeliveryReportsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "driver",
                table: "filpride_delivery_reports");

            migrationBuilder.RenameColumn(
                name: "truck_and_plate_no",
                table: "filpride_delivery_reports",
                newName: "load_port");

            migrationBuilder.AddColumn<decimal>(
                name: "freight",
                table: "filpride_delivery_reports",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "purchase_order_id",
                table: "filpride_delivery_reports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_reports_purchase_order_id",
                table: "filpride_delivery_reports",
                column: "purchase_order_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_delivery_reports_filpride_purchase_orders_purchase",
                table: "filpride_delivery_reports",
                column: "purchase_order_id",
                principalTable: "filpride_purchase_orders",
                principalColumn: "purchase_order_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_delivery_reports_filpride_purchase_orders_purchase",
                table: "filpride_delivery_reports");

            migrationBuilder.DropIndex(
                name: "ix_filpride_delivery_reports_purchase_order_id",
                table: "filpride_delivery_reports");

            migrationBuilder.DropColumn(
                name: "freight",
                table: "filpride_delivery_reports");

            migrationBuilder.DropColumn(
                name: "purchase_order_id",
                table: "filpride_delivery_reports");

            migrationBuilder.RenameColumn(
                name: "load_port",
                table: "filpride_delivery_reports",
                newName: "truck_and_plate_no");

            migrationBuilder.AddColumn<string>(
                name: "driver",
                table: "filpride_delivery_reports",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "");
        }
    }
}
