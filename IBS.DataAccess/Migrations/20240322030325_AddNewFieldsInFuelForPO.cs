using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddNewFieldsInFuelForPO : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SupplierCode",
                table: "Suppliers",
                type: "varchar(7)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(7)");

            migrationBuilder.AddColumn<string>(
                name: "dtllink",
                table: "LubeDeliveries",
                type: "varchar(10)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "cust",
                table: "Fuels",
                type: "varchar(20)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "plateno",
                table: "Fuels",
                type: "varchar(20)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "pono",
                table: "Fuels",
                type: "varchar(20)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "dtllink",
                table: "LubeDeliveries");

            migrationBuilder.DropColumn(
                name: "cust",
                table: "Fuels");

            migrationBuilder.DropColumn(
                name: "plateno",
                table: "Fuels");

            migrationBuilder.DropColumn(
                name: "pono",
                table: "Fuels");

            migrationBuilder.AlterColumn<string>(
                name: "SupplierCode",
                table: "Suppliers",
                type: "varchar(7)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(7)",
                oldNullable: true);
        }
    }
}
