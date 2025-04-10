using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNotNeededFieldsInMobilityReceivingReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "driver",
                table: "mobility_receiving_reports");

            migrationBuilder.DropColumn(
                name: "plate_no",
                table: "mobility_receiving_reports");

            migrationBuilder.DropColumn(
                name: "received_quantity",
                table: "mobility_receiving_reports");

            migrationBuilder.AlterColumn<int>(
                name: "delivery_receipt_id",
                table: "mobility_receiving_reports",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "delivery_receipt_id",
                table: "mobility_receiving_reports",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "driver",
                table: "mobility_receiving_reports",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "plate_no",
                table: "mobility_receiving_reports",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "received_quantity",
                table: "mobility_receiving_reports",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
