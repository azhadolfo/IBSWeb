using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ChangeSomeUncessaryIntToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PosCode",
                table: "Stations",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "StationPosCode",
                table: "SalesHeaders",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeNo",
                table: "LubePurchaseHeaders",
                type: "varchar(5)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ShiftNo",
                table: "LubePurchaseHeaders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ShiftRecId",
                table: "LubePurchaseHeaders",
                type: "varchar(20)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StationCode",
                table: "LubePurchaseHeaders",
                type: "varchar(5)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "StationPosCode",
                table: "GeneralLedgers",
                type: "varchar(5)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "StationPosCode",
                table: "FuelPurchase",
                type: "varchar(5)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "EmployeeNo",
                table: "FuelPurchase",
                type: "varchar(5)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmployeeNo",
                table: "LubePurchaseHeaders");

            migrationBuilder.DropColumn(
                name: "ShiftNo",
                table: "LubePurchaseHeaders");

            migrationBuilder.DropColumn(
                name: "ShiftRecId",
                table: "LubePurchaseHeaders");

            migrationBuilder.DropColumn(
                name: "StationCode",
                table: "LubePurchaseHeaders");

            migrationBuilder.AlterColumn<int>(
                name: "PosCode",
                table: "Stations",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "StationPosCode",
                table: "SalesHeaders",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "StationPosCode",
                table: "GeneralLedgers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(5)");

            migrationBuilder.AlterColumn<int>(
                name: "StationPosCode",
                table: "FuelPurchase",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(5)");

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeNo",
                table: "FuelPurchase",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(5)");
        }
    }
}
