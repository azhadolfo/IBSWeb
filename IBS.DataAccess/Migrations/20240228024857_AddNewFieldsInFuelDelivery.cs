using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddNewFieldsInFuelDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmpNo",
                table: "FuelDeliveries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ShiftNumber",
                table: "FuelDeliveries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ShiftRecId",
                table: "FuelDeliveries",
                type: "varchar(20)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StnCode",
                table: "FuelDeliveries",
                type: "varchar(5)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmpNo",
                table: "FuelDeliveries");

            migrationBuilder.DropColumn(
                name: "ShiftNumber",
                table: "FuelDeliveries");

            migrationBuilder.DropColumn(
                name: "ShiftRecId",
                table: "FuelDeliveries");

            migrationBuilder.DropColumn(
                name: "StnCode",
                table: "FuelDeliveries");
        }
    }
}
