using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameEmpnoToCashierCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "empno",
                table: "LubeDeliveries",
                newName: "cashiercode");

            migrationBuilder.RenameColumn(
                name: "empno",
                table: "FuelDeliveries",
                newName: "cashiercode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "cashiercode",
                table: "LubeDeliveries",
                newName: "empno");

            migrationBuilder.RenameColumn(
                name: "cashiercode",
                table: "FuelDeliveries",
                newName: "empno");
        }
    }
}
