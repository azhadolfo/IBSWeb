using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ChangeFromIntToGuidSomeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FuelDeliveries",
                columns: table => new
                {
                    FuelDeliveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    shiftrecid = table.Column<string>(type: "varchar(20)", nullable: false),
                    stncode = table.Column<string>(type: "varchar(5)", nullable: false),
                    cashiercode = table.Column<string>(type: "text", nullable: false),
                    shiftnumber = table.Column<int>(type: "integer", nullable: false),
                    deliverydate = table.Column<DateOnly>(type: "date", nullable: false),
                    timein = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    timeout = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    driver = table.Column<string>(type: "varchar(100)", nullable: false),
                    hauler = table.Column<string>(type: "varchar(100)", nullable: false),
                    platenumber = table.Column<string>(type: "varchar(50)", nullable: false),
                    drnumber = table.Column<string>(type: "varchar(50)", nullable: false),
                    wcnumber = table.Column<string>(type: "varchar(50)", nullable: false),
                    tanknumber = table.Column<int>(type: "integer", nullable: false),
                    productcode = table.Column<string>(type: "varchar(10)", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    purchaseprice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    sellprice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    volumebefore = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    volumeafter = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    receivedby = table.Column<string>(type: "varchar(50)", nullable: false),
                    createdby = table.Column<string>(type: "varchar(50)", nullable: false),
                    createddate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelDeliveries", x => x.FuelDeliveryId);
                });

            migrationBuilder.CreateTable(
                name: "Fuels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Start = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    End = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    INV_DATE = table.Column<DateOnly>(type: "date", nullable: false),
                    xCORPCODE = table.Column<int>(type: "integer", nullable: false),
                    xSITECODE = table.Column<int>(type: "integer", nullable: false),
                    xTANK = table.Column<int>(type: "integer", nullable: false),
                    xPUMP = table.Column<int>(type: "integer", nullable: false),
                    xNOZZLE = table.Column<int>(type: "integer", nullable: false),
                    xYEAR = table.Column<int>(type: "integer", nullable: false),
                    xMONTH = table.Column<int>(type: "integer", nullable: false),
                    xDAY = table.Column<int>(type: "integer", nullable: false),
                    xTRANSACTION = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AmountDB = table.Column<decimal>(type: "numeric", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Calibration = table.Column<decimal>(type: "numeric", nullable: false),
                    Volume = table.Column<double>(type: "double precision", nullable: false),
                    ItemCode = table.Column<string>(type: "varchar(16)", nullable: false),
                    Particulars = table.Column<string>(type: "varchar(32)", nullable: false),
                    Opening = table.Column<double>(type: "double precision", nullable: false),
                    Closing = table.Column<double>(type: "double precision", nullable: false),
                    nozdown = table.Column<string>(type: "varchar(20)", nullable: false),
                    InTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    OutTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Liters = table.Column<double>(type: "double precision", nullable: false),
                    xOID = table.Column<string>(type: "varchar(20)", nullable: false),
                    xONAME = table.Column<string>(type: "varchar(20)", nullable: false),
                    Shift = table.Column<int>(type: "integer", nullable: false),
                    plateno = table.Column<string>(type: "varchar(20)", nullable: true),
                    pono = table.Column<string>(type: "varchar(20)", nullable: true),
                    cust = table.Column<string>(type: "varchar(20)", nullable: true),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TransCount = table.Column<int>(type: "integer", nullable: false),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EditedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    EditedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CanceledBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    CanceledDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    VoidedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PostedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    PostedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fuels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LubeDeliveries",
                columns: table => new
                {
                    LubeDeliveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    shiftrecid = table.Column<string>(type: "varchar(20)", nullable: false),
                    stncode = table.Column<string>(type: "varchar(5)", nullable: false),
                    cashiercode = table.Column<string>(type: "text", nullable: false),
                    shiftnumber = table.Column<int>(type: "integer", nullable: false),
                    deliverydate = table.Column<DateOnly>(type: "date", nullable: false),
                    suppliercode = table.Column<string>(type: "varchar(10)", nullable: false),
                    invoiceno = table.Column<string>(type: "varchar(50)", nullable: false),
                    drno = table.Column<string>(type: "varchar(50)", nullable: false),
                    pono = table.Column<string>(type: "varchar(50)", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    rcvdby = table.Column<string>(type: "varchar(50)", nullable: false),
                    createdby = table.Column<string>(type: "varchar(50)", nullable: false),
                    createddate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    dtllink = table.Column<string>(type: "varchar(50)", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit = table.Column<string>(type: "varchar(10)", nullable: false),
                    description = table.Column<string>(type: "varchar(200)", nullable: false),
                    unitprice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    productcode = table.Column<string>(type: "varchar(10)", nullable: false),
                    piece = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LubeDeliveries", x => x.LubeDeliveryId);
                });

            migrationBuilder.CreateTable(
                name: "Lubes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    INV_DATE = table.Column<DateOnly>(type: "date", nullable: false),
                    xYEAR = table.Column<int>(type: "integer", nullable: false),
                    xMONTH = table.Column<int>(type: "integer", nullable: false),
                    xDAY = table.Column<int>(type: "integer", nullable: false),
                    xCORPCODE = table.Column<int>(type: "integer", nullable: false),
                    xSITECODE = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AmountDB = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LubesQty = table.Column<int>(type: "integer", nullable: false),
                    ItemCode = table.Column<string>(type: "varchar(16)", nullable: false),
                    Particulars = table.Column<string>(type: "varchar(100)", nullable: false),
                    xOID = table.Column<string>(type: "varchar(10)", nullable: false),
                    Cashier = table.Column<string>(type: "varchar(20)", nullable: false),
                    Shift = table.Column<int>(type: "integer", nullable: false),
                    xTRANSACTION = table.Column<long>(type: "bigint", nullable: false),
                    xStamp = table.Column<string>(type: "varchar(50)", nullable: false),
                    plateno = table.Column<string>(type: "varchar(20)", nullable: true),
                    pono = table.Column<string>(type: "varchar(20)", nullable: true),
                    cust = table.Column<string>(type: "varchar(20)", nullable: true),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EditedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    EditedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CanceledBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    CanceledDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    VoidedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PostedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    PostedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lubes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Offlines",
                columns: table => new
                {
                    OfflineId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeriesNo = table.Column<int>(type: "integer", nullable: false),
                    StationCode = table.Column<string>(type: "varchar(3)", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Product = table.Column<string>(type: "varchar(20)", nullable: false),
                    Pump = table.Column<int>(type: "integer", nullable: false),
                    Opening = table.Column<double>(type: "double precision", nullable: false),
                    Closing = table.Column<double>(type: "double precision", nullable: false),
                    Liters = table.Column<double>(type: "double precision", nullable: false),
                    Balance = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offlines", x => x.OfflineId);
                });

            migrationBuilder.CreateTable(
                name: "PoSalesRaw",
                columns: table => new
                {
                    POSalesRawId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_PoSalesRaw", x => x.POSalesRawId);
                });

            migrationBuilder.CreateTable(
                name: "SafeDrops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    INV_DATE = table.Column<DateOnly>(type: "date", nullable: false),
                    BDate = table.Column<DateOnly>(type: "date", nullable: false),
                    xYEAR = table.Column<int>(type: "integer", nullable: false),
                    xMONTH = table.Column<int>(type: "integer", nullable: false),
                    xDAY = table.Column<int>(type: "integer", nullable: false),
                    xCORPCODE = table.Column<int>(type: "integer", nullable: false),
                    xSITECODE = table.Column<int>(type: "integer", nullable: false),
                    TTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    xSTAMP = table.Column<string>(type: "varchar(50)", nullable: false),
                    xOID = table.Column<string>(type: "varchar(10)", nullable: false),
                    xONAME = table.Column<string>(type: "varchar(20)", nullable: false),
                    Shift = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EditedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    EditedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CanceledBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    CanceledDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    VoidedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PostedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    PostedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SafeDrops", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FuelDeliveries_shiftrecid",
                table: "FuelDeliveries",
                column: "shiftrecid");

            migrationBuilder.CreateIndex(
                name: "IX_FuelDeliveries_stncode",
                table: "FuelDeliveries",
                column: "stncode");

            migrationBuilder.CreateIndex(
                name: "IX_Fuels_INV_DATE",
                table: "Fuels",
                column: "INV_DATE");

            migrationBuilder.CreateIndex(
                name: "IX_Fuels_IsProcessed",
                table: "Fuels",
                column: "IsProcessed");

            migrationBuilder.CreateIndex(
                name: "IX_Fuels_ItemCode",
                table: "Fuels",
                column: "ItemCode");

            migrationBuilder.CreateIndex(
                name: "IX_Fuels_Particulars",
                table: "Fuels",
                column: "Particulars");

            migrationBuilder.CreateIndex(
                name: "IX_Fuels_Shift",
                table: "Fuels",
                column: "Shift");

            migrationBuilder.CreateIndex(
                name: "IX_Fuels_xONAME",
                table: "Fuels",
                column: "xONAME");

            migrationBuilder.CreateIndex(
                name: "IX_Fuels_xPUMP",
                table: "Fuels",
                column: "xPUMP");

            migrationBuilder.CreateIndex(
                name: "IX_Fuels_xSITECODE",
                table: "Fuels",
                column: "xSITECODE");

            migrationBuilder.CreateIndex(
                name: "IX_LubeDeliveries_dtllink",
                table: "LubeDeliveries",
                column: "dtllink");

            migrationBuilder.CreateIndex(
                name: "IX_LubeDeliveries_shiftrecid",
                table: "LubeDeliveries",
                column: "shiftrecid");

            migrationBuilder.CreateIndex(
                name: "IX_LubeDeliveries_stncode",
                table: "LubeDeliveries",
                column: "stncode");

            migrationBuilder.CreateIndex(
                name: "IX_Lubes_Cashier",
                table: "Lubes",
                column: "Cashier");

            migrationBuilder.CreateIndex(
                name: "IX_Lubes_INV_DATE",
                table: "Lubes",
                column: "INV_DATE");

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

            migrationBuilder.CreateIndex(
                name: "IX_SafeDrops_INV_DATE",
                table: "SafeDrops",
                column: "INV_DATE");

            migrationBuilder.CreateIndex(
                name: "IX_SafeDrops_xONAME",
                table: "SafeDrops",
                column: "xONAME");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FuelDeliveries");

            migrationBuilder.DropTable(
                name: "Fuels");

            migrationBuilder.DropTable(
                name: "LubeDeliveries");

            migrationBuilder.DropTable(
                name: "Lubes");

            migrationBuilder.DropTable(
                name: "Offlines");

            migrationBuilder.DropTable(
                name: "PoSalesRaw");

            migrationBuilder.DropTable(
                name: "SafeDrops");
        }
    }
}
