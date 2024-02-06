using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTheNameOfFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NozDown",
                table: "Fuels",
                newName: "nozDown");

            migrationBuilder.RenameColumn(
                name: "Year",
                table: "Fuels",
                newName: "xYEAR");

            migrationBuilder.RenameColumn(
                name: "Transaction",
                table: "Fuels",
                newName: "xTRANSACTION");

            migrationBuilder.RenameColumn(
                name: "Tank",
                table: "Fuels",
                newName: "xTANK");

            migrationBuilder.RenameColumn(
                name: "SiteCode",
                table: "Fuels",
                newName: "xSITECODE");

            migrationBuilder.RenameColumn(
                name: "Pump",
                table: "Fuels",
                newName: "xPUMP");

            migrationBuilder.RenameColumn(
                name: "Nozzle",
                table: "Fuels",
                newName: "xNOZZLE");

            migrationBuilder.RenameColumn(
                name: "Month",
                table: "Fuels",
                newName: "xMONTH");

            migrationBuilder.RenameColumn(
                name: "Day",
                table: "Fuels",
                newName: "xDAY");

            migrationBuilder.RenameColumn(
                name: "CorpCode",
                table: "Fuels",
                newName: "xCORPCODE");

            migrationBuilder.RenameColumn(
                name: "CashierName",
                table: "Fuels",
                newName: "xONAME");

            migrationBuilder.RenameColumn(
                name: "CashierId",
                table: "Fuels",
                newName: "xOID");

            migrationBuilder.RenameColumn(
                name: "InvoiceDate",
                table: "Fuels",
                newName: "INV_DATE");

            migrationBuilder.AlterColumn<string>(
                name: "xONAME",
                table: "Fuels",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)");

            migrationBuilder.AlterColumn<string>(
                name: "xOID",
                table: "Fuels",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(10)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "nozDown",
                table: "Fuels",
                newName: "NozDown");

            migrationBuilder.RenameColumn(
                name: "xYEAR",
                table: "Fuels",
                newName: "Year");

            migrationBuilder.RenameColumn(
                name: "xTRANSACTION",
                table: "Fuels",
                newName: "Transaction");

            migrationBuilder.RenameColumn(
                name: "xTANK",
                table: "Fuels",
                newName: "Tank");

            migrationBuilder.RenameColumn(
                name: "xSITECODE",
                table: "Fuels",
                newName: "SiteCode");

            migrationBuilder.RenameColumn(
                name: "xPUMP",
                table: "Fuels",
                newName: "Pump");

            migrationBuilder.RenameColumn(
                name: "xONAME",
                table: "Fuels",
                newName: "CashierName");

            migrationBuilder.RenameColumn(
                name: "xOID",
                table: "Fuels",
                newName: "CashierId");

            migrationBuilder.RenameColumn(
                name: "xNOZZLE",
                table: "Fuels",
                newName: "Nozzle");

            migrationBuilder.RenameColumn(
                name: "xMONTH",
                table: "Fuels",
                newName: "Month");

            migrationBuilder.RenameColumn(
                name: "xDAY",
                table: "Fuels",
                newName: "Day");

            migrationBuilder.RenameColumn(
                name: "xCORPCODE",
                table: "Fuels",
                newName: "CorpCode");

            migrationBuilder.RenameColumn(
                name: "INV_DATE",
                table: "Fuels",
                newName: "InvoiceDate");

            migrationBuilder.AlterColumn<string>(
                name: "CashierName",
                table: "Fuels",
                type: "varchar(20)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CashierId",
                table: "Fuels",
                type: "varchar(10)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
