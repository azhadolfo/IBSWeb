using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CreateTableForPurchaseOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "POSales",
                columns: table => new
                {
                    POSalesId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    shiftrecid = table.Column<string>(type: "varchar(20)", nullable: false),
                    stncode = table.Column<string>(type: "varchar(5)", nullable: false),
                    cashiercode = table.Column<string>(type: "text", nullable: false),
                    shiftnumber = table.Column<int>(type: "integer", nullable: false),
                    podate = table.Column<DateOnly>(type: "date", nullable: false),
                    potime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
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
                    table.PrimaryKey("PK_POSales", x => x.POSalesId);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    PurchaseOrderId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PurchaseOrderNo = table.Column<string>(type: "varchar(50)", nullable: false),
                    ShiftRecId = table.Column<string>(type: "varchar(20)", nullable: false),
                    StationCode = table.Column<string>(type: "varchar(5)", nullable: false),
                    CashierCode = table.Column<string>(type: "varchar(5)", nullable: false),
                    ShiftNo = table.Column<int>(type: "integer", nullable: false),
                    PurchaseOrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PurchaseOrderTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    CustomerCode = table.Column<string>(type: "varchar(20)", nullable: false),
                    Driver = table.Column<string>(type: "varchar(50)", nullable: false),
                    PlateNo = table.Column<string>(type: "varchar(50)", nullable: false),
                    DrNo = table.Column<string>(type: "varchar(50)", nullable: false),
                    TripTicket = table.Column<string>(type: "varchar(20)", nullable: false),
                    ProductCode = table.Column<string>(type: "varchar(10)", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ContractPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_PurchaseOrders", x => x.PurchaseOrderId);
                });

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
                name: "IX_FuelDeliveries_shiftrecid",
                table: "FuelDeliveries",
                column: "shiftrecid");

            migrationBuilder.CreateIndex(
                name: "IX_FuelDeliveries_stncode",
                table: "FuelDeliveries",
                column: "stncode");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "POSales");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_LubeDeliveries_dtllink",
                table: "LubeDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_LubeDeliveries_shiftrecid",
                table: "LubeDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_LubeDeliveries_stncode",
                table: "LubeDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_FuelDeliveries_shiftrecid",
                table: "FuelDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_FuelDeliveries_stncode",
                table: "FuelDeliveries");
        }
    }
}
