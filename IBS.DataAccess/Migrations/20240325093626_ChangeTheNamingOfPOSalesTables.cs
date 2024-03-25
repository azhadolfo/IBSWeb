using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTheNamingOfPOSalesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_POSales_shiftrecid",
                table: "POSales");

            migrationBuilder.DropIndex(
                name: "IX_POSales_stncode",
                table: "POSales");

            migrationBuilder.DropIndex(
                name: "IX_POSales_tripticket",
                table: "POSales");

            migrationBuilder.RenameColumn(
                name: "tripticket",
                table: "POSales",
                newName: "TripTicket");

            migrationBuilder.RenameColumn(
                name: "shiftrecid",
                table: "POSales",
                newName: "ShiftRecId");

            migrationBuilder.RenameColumn(
                name: "quantity",
                table: "POSales",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "productcode",
                table: "POSales",
                newName: "ProductCode");

            migrationBuilder.RenameColumn(
                name: "price",
                table: "POSales",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "plateno",
                table: "POSales",
                newName: "PlateNo");

            migrationBuilder.RenameColumn(
                name: "driver",
                table: "POSales",
                newName: "Driver");

            migrationBuilder.RenameColumn(
                name: "customercode",
                table: "POSales",
                newName: "CustomerCode");

            migrationBuilder.RenameColumn(
                name: "createddate",
                table: "POSales",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "createdby",
                table: "POSales",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "contractprice",
                table: "POSales",
                newName: "ContractPrice");

            migrationBuilder.RenameColumn(
                name: "cashiercode",
                table: "POSales",
                newName: "CashierCode");

            migrationBuilder.RenameColumn(
                name: "stncode",
                table: "POSales",
                newName: "StationCode");

            migrationBuilder.RenameColumn(
                name: "shiftnumber",
                table: "POSales",
                newName: "ShiftNo");

            migrationBuilder.RenameColumn(
                name: "potime",
                table: "POSales",
                newName: "PurchaseOrderTime");

            migrationBuilder.RenameColumn(
                name: "podate",
                table: "POSales",
                newName: "PurchaseOrderDate");

            migrationBuilder.RenameColumn(
                name: "drnumber",
                table: "POSales",
                newName: "PurchaseOrderNo");

            migrationBuilder.RenameColumn(
                name: "POSalesId",
                table: "POSales",
                newName: "PurchaseOrderId");

            migrationBuilder.AlterColumn<string>(
                name: "TripTicket",
                table: "POSales",
                type: "varchar(20)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "POSales",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "POSales",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AlterColumn<string>(
                name: "CashierCode",
                table: "POSales",
                type: "varchar(5)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "CanceledBy",
                table: "POSales",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CanceledDate",
                table: "POSales",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DrNo",
                table: "POSales",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EditedBy",
                table: "POSales",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EditedDate",
                table: "POSales",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostedBy",
                table: "POSales",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PostedDate",
                table: "POSales",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidedBy",
                table: "POSales",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedDate",
                table: "POSales",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PoSalesRaw",
                columns: table => new
                {
                    POSalesId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    shiftrecid = table.Column<string>(type: "varchar(20)", nullable: false),
                    stncode = table.Column<string>(type: "varchar(5)", nullable: false),
                    cashiercode = table.Column<string>(type: "text", nullable: false),
                    shiftnumber = table.Column<int>(type: "integer", nullable: false),
                    podate = table.Column<DateOnly>(type: "date", nullable: false),
                    potime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    customercode = table.Column<string>(type: "varchar(20)", nullable: false),
                    driver = table.Column<string>(type: "varchar(50)", nullable: false),
                    plateno = table.Column<string>(type: "varchar(50)", nullable: false),
                    drnumber = table.Column<string>(type: "varchar(50)", nullable: false),
                    tripticket = table.Column<string>(type: "text", nullable: false),
                    productcode = table.Column<string>(type: "varchar(10)", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    contractprice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    createdby = table.Column<string>(type: "varchar(50)", nullable: false),
                    createddate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoSalesRaw", x => x.POSalesId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_POSales_PurchaseOrderNo",
                table: "POSales",
                column: "PurchaseOrderNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PoSalesRaw_shiftrecid",
                table: "PoSalesRaw",
                column: "shiftrecid");

            migrationBuilder.CreateIndex(
                name: "IX_PoSalesRaw_stncode",
                table: "PoSalesRaw",
                column: "stncode");

            migrationBuilder.CreateIndex(
                name: "IX_PoSalesRaw_tripticket",
                table: "PoSalesRaw",
                column: "tripticket");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PoSalesRaw");

            migrationBuilder.DropIndex(
                name: "IX_POSales_PurchaseOrderNo",
                table: "POSales");

            migrationBuilder.DropColumn(
                name: "CanceledBy",
                table: "POSales");

            migrationBuilder.DropColumn(
                name: "CanceledDate",
                table: "POSales");

            migrationBuilder.DropColumn(
                name: "DrNo",
                table: "POSales");

            migrationBuilder.DropColumn(
                name: "EditedBy",
                table: "POSales");

            migrationBuilder.DropColumn(
                name: "EditedDate",
                table: "POSales");

            migrationBuilder.DropColumn(
                name: "PostedBy",
                table: "POSales");

            migrationBuilder.DropColumn(
                name: "PostedDate",
                table: "POSales");

            migrationBuilder.DropColumn(
                name: "VoidedBy",
                table: "POSales");

            migrationBuilder.DropColumn(
                name: "VoidedDate",
                table: "POSales");

            migrationBuilder.RenameColumn(
                name: "TripTicket",
                table: "POSales",
                newName: "tripticket");

            migrationBuilder.RenameColumn(
                name: "ShiftRecId",
                table: "POSales",
                newName: "shiftrecid");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "POSales",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "ProductCode",
                table: "POSales",
                newName: "productcode");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "POSales",
                newName: "price");

            migrationBuilder.RenameColumn(
                name: "PlateNo",
                table: "POSales",
                newName: "plateno");

            migrationBuilder.RenameColumn(
                name: "Driver",
                table: "POSales",
                newName: "driver");

            migrationBuilder.RenameColumn(
                name: "CustomerCode",
                table: "POSales",
                newName: "customercode");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "POSales",
                newName: "createddate");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "POSales",
                newName: "createdby");

            migrationBuilder.RenameColumn(
                name: "ContractPrice",
                table: "POSales",
                newName: "contractprice");

            migrationBuilder.RenameColumn(
                name: "CashierCode",
                table: "POSales",
                newName: "cashiercode");

            migrationBuilder.RenameColumn(
                name: "StationCode",
                table: "POSales",
                newName: "stncode");

            migrationBuilder.RenameColumn(
                name: "ShiftNo",
                table: "POSales",
                newName: "shiftnumber");

            migrationBuilder.RenameColumn(
                name: "PurchaseOrderTime",
                table: "POSales",
                newName: "potime");

            migrationBuilder.RenameColumn(
                name: "PurchaseOrderNo",
                table: "POSales",
                newName: "drnumber");

            migrationBuilder.RenameColumn(
                name: "PurchaseOrderDate",
                table: "POSales",
                newName: "podate");

            migrationBuilder.RenameColumn(
                name: "PurchaseOrderId",
                table: "POSales",
                newName: "POSalesId");

            migrationBuilder.AlterColumn<string>(
                name: "tripticket",
                table: "POSales",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "createddate",
                table: "POSales",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "createdby",
                table: "POSales",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "cashiercode",
                table: "POSales",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(5)");

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    PurchaseOrderId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CanceledBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    CanceledDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CashierCode = table.Column<string>(type: "varchar(5)", nullable: false),
                    ContractPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustomerCode = table.Column<string>(type: "varchar(20)", nullable: false),
                    DrNo = table.Column<string>(type: "varchar(50)", nullable: false),
                    Driver = table.Column<string>(type: "varchar(50)", nullable: false),
                    EditedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    EditedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PlateNo = table.Column<string>(type: "varchar(50)", nullable: false),
                    PostedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    PostedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ProductCode = table.Column<string>(type: "varchar(10)", nullable: false),
                    PurchaseOrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PurchaseOrderNo = table.Column<string>(type: "varchar(50)", nullable: false),
                    PurchaseOrderTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ShiftNo = table.Column<int>(type: "integer", nullable: false),
                    ShiftRecId = table.Column<string>(type: "varchar(20)", nullable: false),
                    StationCode = table.Column<string>(type: "varchar(5)", nullable: false),
                    TripTicket = table.Column<string>(type: "varchar(20)", nullable: false),
                    VoidedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    VoidedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.PurchaseOrderId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_POSales_shiftrecid",
                table: "POSales",
                column: "shiftrecid");

            migrationBuilder.CreateIndex(
                name: "IX_POSales_stncode",
                table: "POSales",
                column: "stncode");

            migrationBuilder.CreateIndex(
                name: "IX_POSales_tripticket",
                table: "POSales",
                column: "tripticket");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_PurchaseOrderNo",
                table: "PurchaseOrders",
                column: "PurchaseOrderNo",
                unique: true);
        }
    }
}
