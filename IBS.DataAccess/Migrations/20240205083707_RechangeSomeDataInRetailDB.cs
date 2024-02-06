using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RechangeSomeDataInRetailDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateTimeStamp",
                table: "SafeDrops");

            migrationBuilder.DropColumn(
                name: "TransactionTime",
                table: "SafeDrops");

            migrationBuilder.RenameColumn(
                name: "Year",
                table: "SafeDrops",
                newName: "xYEAR");

            migrationBuilder.RenameColumn(
                name: "SiteCode",
                table: "SafeDrops",
                newName: "xSITECODE");

            migrationBuilder.RenameColumn(
                name: "Month",
                table: "SafeDrops",
                newName: "xMONTH");

            migrationBuilder.RenameColumn(
                name: "InvoiceDate",
                table: "SafeDrops",
                newName: "INV_DATE");

            migrationBuilder.RenameColumn(
                name: "Day",
                table: "SafeDrops",
                newName: "xDAY");

            migrationBuilder.RenameColumn(
                name: "CorpCode",
                table: "SafeDrops",
                newName: "xCORPCODE");

            migrationBuilder.RenameColumn(
                name: "CashierName",
                table: "SafeDrops",
                newName: "xONAME");

            migrationBuilder.RenameColumn(
                name: "CashierId",
                table: "SafeDrops",
                newName: "xOID");

            migrationBuilder.RenameColumn(
                name: "Year",
                table: "Lubes",
                newName: "xYEAR");

            migrationBuilder.RenameColumn(
                name: "Volume",
                table: "Lubes",
                newName: "LubesQty");

            migrationBuilder.RenameColumn(
                name: "Transaction",
                table: "Lubes",
                newName: "xTRANSACTION");

            migrationBuilder.RenameColumn(
                name: "SiteCode",
                table: "Lubes",
                newName: "xSITECODE");

            migrationBuilder.RenameColumn(
                name: "Month",
                table: "Lubes",
                newName: "xMONTH");

            migrationBuilder.RenameColumn(
                name: "InvoiceDate",
                table: "Lubes",
                newName: "INV_DATE");

            migrationBuilder.RenameColumn(
                name: "Day",
                table: "Lubes",
                newName: "xDAY");

            migrationBuilder.RenameColumn(
                name: "CorpCode",
                table: "Lubes",
                newName: "xCORPCODE");

            migrationBuilder.RenameColumn(
                name: "CashierName",
                table: "Lubes",
                newName: "Cashier");

            migrationBuilder.RenameColumn(
                name: "CashierId",
                table: "Lubes",
                newName: "xOID");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "TTime",
                table: "SafeDrops",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<string>(
                name: "xSTAMP",
                table: "SafeDrops",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "xStamp",
                table: "Lubes",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TTime",
                table: "SafeDrops");

            migrationBuilder.DropColumn(
                name: "xSTAMP",
                table: "SafeDrops");

            migrationBuilder.DropColumn(
                name: "xStamp",
                table: "Lubes");

            migrationBuilder.RenameColumn(
                name: "xYEAR",
                table: "SafeDrops",
                newName: "Year");

            migrationBuilder.RenameColumn(
                name: "xSITECODE",
                table: "SafeDrops",
                newName: "SiteCode");

            migrationBuilder.RenameColumn(
                name: "xONAME",
                table: "SafeDrops",
                newName: "CashierName");

            migrationBuilder.RenameColumn(
                name: "xOID",
                table: "SafeDrops",
                newName: "CashierId");

            migrationBuilder.RenameColumn(
                name: "xMONTH",
                table: "SafeDrops",
                newName: "Month");

            migrationBuilder.RenameColumn(
                name: "xDAY",
                table: "SafeDrops",
                newName: "Day");

            migrationBuilder.RenameColumn(
                name: "xCORPCODE",
                table: "SafeDrops",
                newName: "CorpCode");

            migrationBuilder.RenameColumn(
                name: "INV_DATE",
                table: "SafeDrops",
                newName: "InvoiceDate");

            migrationBuilder.RenameColumn(
                name: "xYEAR",
                table: "Lubes",
                newName: "Year");

            migrationBuilder.RenameColumn(
                name: "xTRANSACTION",
                table: "Lubes",
                newName: "Transaction");

            migrationBuilder.RenameColumn(
                name: "xSITECODE",
                table: "Lubes",
                newName: "SiteCode");

            migrationBuilder.RenameColumn(
                name: "xOID",
                table: "Lubes",
                newName: "CashierId");

            migrationBuilder.RenameColumn(
                name: "xMONTH",
                table: "Lubes",
                newName: "Month");

            migrationBuilder.RenameColumn(
                name: "xDAY",
                table: "Lubes",
                newName: "Day");

            migrationBuilder.RenameColumn(
                name: "xCORPCODE",
                table: "Lubes",
                newName: "CorpCode");

            migrationBuilder.RenameColumn(
                name: "LubesQty",
                table: "Lubes",
                newName: "Volume");

            migrationBuilder.RenameColumn(
                name: "INV_DATE",
                table: "Lubes",
                newName: "InvoiceDate");

            migrationBuilder.RenameColumn(
                name: "Cashier",
                table: "Lubes",
                newName: "CashierName");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateTimeStamp",
                table: "SafeDrops",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "TransactionTime",
                table: "SafeDrops",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "DatetimeStamp",
                table: "Lubes",
                type: "varchar(200)",
                nullable: false,
                defaultValue: "");
        }
    }
}
