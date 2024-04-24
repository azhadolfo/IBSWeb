using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RecreatePurchasesEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LubeDeliveryDetails");

            migrationBuilder.DropTable(
                name: "LubeDeliveryHeaders");

            migrationBuilder.DropColumn(
                name: "CanceledBy",
                table: "FuelDeliveries");

            migrationBuilder.DropColumn(
                name: "CanceledDate",
                table: "FuelDeliveries");

            migrationBuilder.DropColumn(
                name: "EditedBy",
                table: "FuelDeliveries");

            migrationBuilder.DropColumn(
                name: "EditedDate",
                table: "FuelDeliveries");

            migrationBuilder.DropColumn(
                name: "GainOrLoss",
                table: "FuelDeliveries");

            migrationBuilder.DropColumn(
                name: "PlateNo",
                table: "FuelDeliveries");

            migrationBuilder.DropColumn(
                name: "PostedBy",
                table: "FuelDeliveries");

            migrationBuilder.DropColumn(
                name: "PostedDate",
                table: "FuelDeliveries");

            migrationBuilder.DropColumn(
                name: "StationCode",
                table: "FuelDeliveries");

            migrationBuilder.DropColumn(
                name: "StationPosCode",
                table: "FuelDeliveries");

            migrationBuilder.DropColumn(
                name: "VoidedBy",
                table: "FuelDeliveries");

            migrationBuilder.DropColumn(
                name: "VoidedDate",
                table: "FuelDeliveries");

            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "LubeDeliveries",
                newName: "unitprice");

            migrationBuilder.RenameColumn(
                name: "Unit",
                table: "LubeDeliveries",
                newName: "unit");

            migrationBuilder.RenameColumn(
                name: "SupplierCode",
                table: "LubeDeliveries",
                newName: "suppliercode");

            migrationBuilder.RenameColumn(
                name: "StnCode",
                table: "LubeDeliveries",
                newName: "stncode");

            migrationBuilder.RenameColumn(
                name: "ShiftRecId",
                table: "LubeDeliveries",
                newName: "shiftrecid");

            migrationBuilder.RenameColumn(
                name: "ShiftNumber",
                table: "LubeDeliveries",
                newName: "shiftnumber");

            migrationBuilder.RenameColumn(
                name: "ReceivedBy",
                table: "LubeDeliveries",
                newName: "receivedby");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "LubeDeliveries",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "ProductCode",
                table: "LubeDeliveries",
                newName: "productcode");

            migrationBuilder.RenameColumn(
                name: "PoNo",
                table: "LubeDeliveries",
                newName: "pono");

            migrationBuilder.RenameColumn(
                name: "Piece",
                table: "LubeDeliveries",
                newName: "piece");

            migrationBuilder.RenameColumn(
                name: "EmpNo",
                table: "LubeDeliveries",
                newName: "empno");

            migrationBuilder.RenameColumn(
                name: "DrNo",
                table: "LubeDeliveries",
                newName: "drno");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "LubeDeliveries",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "DeliveryDate",
                table: "LubeDeliveries",
                newName: "deliverydate");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "LubeDeliveries",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "TimeIn",
                table: "FuelDeliveries",
                newName: "timein");

            migrationBuilder.RenameColumn(
                name: "StnCode",
                table: "FuelDeliveries",
                newName: "stncode");

            migrationBuilder.RenameColumn(
                name: "ShiftRecId",
                table: "FuelDeliveries",
                newName: "shiftrecid");

            migrationBuilder.RenameColumn(
                name: "ShiftNumber",
                table: "FuelDeliveries",
                newName: "shiftnumber");

            migrationBuilder.RenameColumn(
                name: "ReceivedBy",
                table: "FuelDeliveries",
                newName: "receivedby");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "FuelDeliveries",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "ProductCode",
                table: "FuelDeliveries",
                newName: "productcode");

            migrationBuilder.RenameColumn(
                name: "Hauler",
                table: "FuelDeliveries",
                newName: "hauler");

            migrationBuilder.RenameColumn(
                name: "EmpNo",
                table: "FuelDeliveries",
                newName: "empno");

            migrationBuilder.RenameColumn(
                name: "Driver",
                table: "FuelDeliveries",
                newName: "driver");

            migrationBuilder.RenameColumn(
                name: "DeliveryDate",
                table: "FuelDeliveries",
                newName: "deliverydate");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "FuelDeliveries",
                newName: "createddate");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "FuelDeliveries",
                newName: "createdby");

            migrationBuilder.RenameColumn(
                name: "WcNo",
                table: "FuelDeliveries",
                newName: "wcnumber");

            migrationBuilder.RenameColumn(
                name: "TimeOut",
                table: "FuelDeliveries",
                newName: "timout");

            migrationBuilder.RenameColumn(
                name: "TankNo",
                table: "FuelDeliveries",
                newName: "tanknumber");

            migrationBuilder.RenameColumn(
                name: "ShouldBe",
                table: "FuelDeliveries",
                newName: "volumebefore");

            migrationBuilder.RenameColumn(
                name: "QuantityBefore",
                table: "FuelDeliveries",
                newName: "volumeafter");

            migrationBuilder.RenameColumn(
                name: "QuantityAfter",
                table: "FuelDeliveries",
                newName: "sellprice");

            migrationBuilder.RenameColumn(
                name: "ProductDescription",
                table: "FuelDeliveries",
                newName: "platenumber");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "FuelDeliveries",
                newName: "purchaseprice");

            migrationBuilder.RenameColumn(
                name: "DrNo",
                table: "FuelDeliveries",
                newName: "drnumber");

            migrationBuilder.AlterColumn<string>(
                name: "receivedby",
                table: "FuelDeliveries",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "createddate",
                table: "FuelDeliveries",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "createdby",
                table: "FuelDeliveries",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "FuelPurchase",
                columns: table => new
                {
                    FuelPurchaseId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShiftRecId = table.Column<string>(type: "varchar(20)", nullable: false),
                    StationCode = table.Column<string>(type: "varchar(5)", nullable: false),
                    EmployeeNo = table.Column<int>(type: "integer", nullable: false),
                    ShiftNo = table.Column<int>(type: "integer", nullable: false),
                    DeliveryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeIn = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    TimeOut = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Driver = table.Column<string>(type: "varchar(100)", nullable: false),
                    Hauler = table.Column<string>(type: "varchar(100)", nullable: false),
                    PlateNo = table.Column<string>(type: "varchar(20)", nullable: false),
                    DrNo = table.Column<string>(type: "varchar(10)", nullable: false),
                    WcNo = table.Column<string>(type: "varchar(10)", nullable: false),
                    TankNo = table.Column<int>(type: "integer", nullable: false),
                    ProductCode = table.Column<string>(type: "varchar(10)", nullable: false),
                    ProductDescription = table.Column<string>(type: "varchar(20)", nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SellingPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    QuantityBefore = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    QuantityAfter = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ShouldBe = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    GainOrLoss = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReceivedBy = table.Column<string>(type: "varchar(20)", nullable: false),
                    StationPosCode = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_FuelPurchase", x => x.FuelPurchaseId);
                });

            migrationBuilder.CreateTable(
                name: "LubePurchaseHeaders",
                columns: table => new
                {
                    LubeDeliveryHeaderId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeliveryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SalesInvoice = table.Column<string>(type: "varchar(10)", nullable: false),
                    SupplierCode = table.Column<string>(type: "varchar(10)", nullable: false),
                    SupplierName = table.Column<string>(type: "varchar(200)", nullable: false),
                    DrNo = table.Column<string>(type: "varchar(10)", nullable: false),
                    PoNo = table.Column<string>(type: "varchar(10)", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_LubePurchaseHeaders", x => x.LubeDeliveryHeaderId);
                });

            migrationBuilder.CreateTable(
                name: "LubePurchaseDetails",
                columns: table => new
                {
                    LubeDeliveryDetailId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LubeDeliveryHeaderId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Unit = table.Column<string>(type: "varchar(10)", nullable: false),
                    Description = table.Column<string>(type: "varchar(200)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LubePurchaseDetails", x => x.LubeDeliveryDetailId);
                    table.ForeignKey(
                        name: "FK_LubePurchaseDetails_LubePurchaseHeaders_LubeDeliveryHeaderId",
                        column: x => x.LubeDeliveryHeaderId,
                        principalTable: "LubePurchaseHeaders",
                        principalColumn: "LubeDeliveryHeaderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LubePurchaseDetails_LubeDeliveryHeaderId",
                table: "LubePurchaseDetails",
                column: "LubeDeliveryHeaderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FuelPurchase");

            migrationBuilder.DropTable(
                name: "LubePurchaseDetails");

            migrationBuilder.DropTable(
                name: "LubePurchaseHeaders");

            migrationBuilder.RenameColumn(
                name: "unitprice",
                table: "LubeDeliveries",
                newName: "UnitPrice");

            migrationBuilder.RenameColumn(
                name: "unit",
                table: "LubeDeliveries",
                newName: "Unit");

            migrationBuilder.RenameColumn(
                name: "suppliercode",
                table: "LubeDeliveries",
                newName: "SupplierCode");

            migrationBuilder.RenameColumn(
                name: "stncode",
                table: "LubeDeliveries",
                newName: "StnCode");

            migrationBuilder.RenameColumn(
                name: "shiftrecid",
                table: "LubeDeliveries",
                newName: "ShiftRecId");

            migrationBuilder.RenameColumn(
                name: "shiftnumber",
                table: "LubeDeliveries",
                newName: "ShiftNumber");

            migrationBuilder.RenameColumn(
                name: "receivedby",
                table: "LubeDeliveries",
                newName: "ReceivedBy");

            migrationBuilder.RenameColumn(
                name: "quantity",
                table: "LubeDeliveries",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "productcode",
                table: "LubeDeliveries",
                newName: "ProductCode");

            migrationBuilder.RenameColumn(
                name: "pono",
                table: "LubeDeliveries",
                newName: "PoNo");

            migrationBuilder.RenameColumn(
                name: "piece",
                table: "LubeDeliveries",
                newName: "Piece");

            migrationBuilder.RenameColumn(
                name: "empno",
                table: "LubeDeliveries",
                newName: "EmpNo");

            migrationBuilder.RenameColumn(
                name: "drno",
                table: "LubeDeliveries",
                newName: "DrNo");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "LubeDeliveries",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "deliverydate",
                table: "LubeDeliveries",
                newName: "DeliveryDate");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "LubeDeliveries",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "timein",
                table: "FuelDeliveries",
                newName: "TimeIn");

            migrationBuilder.RenameColumn(
                name: "stncode",
                table: "FuelDeliveries",
                newName: "StnCode");

            migrationBuilder.RenameColumn(
                name: "shiftrecid",
                table: "FuelDeliveries",
                newName: "ShiftRecId");

            migrationBuilder.RenameColumn(
                name: "shiftnumber",
                table: "FuelDeliveries",
                newName: "ShiftNumber");

            migrationBuilder.RenameColumn(
                name: "receivedby",
                table: "FuelDeliveries",
                newName: "ReceivedBy");

            migrationBuilder.RenameColumn(
                name: "quantity",
                table: "FuelDeliveries",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "productcode",
                table: "FuelDeliveries",
                newName: "ProductCode");

            migrationBuilder.RenameColumn(
                name: "hauler",
                table: "FuelDeliveries",
                newName: "Hauler");

            migrationBuilder.RenameColumn(
                name: "empno",
                table: "FuelDeliveries",
                newName: "EmpNo");

            migrationBuilder.RenameColumn(
                name: "driver",
                table: "FuelDeliveries",
                newName: "Driver");

            migrationBuilder.RenameColumn(
                name: "deliverydate",
                table: "FuelDeliveries",
                newName: "DeliveryDate");

            migrationBuilder.RenameColumn(
                name: "createddate",
                table: "FuelDeliveries",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "createdby",
                table: "FuelDeliveries",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "wcnumber",
                table: "FuelDeliveries",
                newName: "WcNo");

            migrationBuilder.RenameColumn(
                name: "volumebefore",
                table: "FuelDeliveries",
                newName: "ShouldBe");

            migrationBuilder.RenameColumn(
                name: "volumeafter",
                table: "FuelDeliveries",
                newName: "QuantityBefore");

            migrationBuilder.RenameColumn(
                name: "timout",
                table: "FuelDeliveries",
                newName: "TimeOut");

            migrationBuilder.RenameColumn(
                name: "tanknumber",
                table: "FuelDeliveries",
                newName: "TankNo");

            migrationBuilder.RenameColumn(
                name: "sellprice",
                table: "FuelDeliveries",
                newName: "QuantityAfter");

            migrationBuilder.RenameColumn(
                name: "purchaseprice",
                table: "FuelDeliveries",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "platenumber",
                table: "FuelDeliveries",
                newName: "ProductDescription");

            migrationBuilder.RenameColumn(
                name: "drnumber",
                table: "FuelDeliveries",
                newName: "DrNo");

            migrationBuilder.AlterColumn<string>(
                name: "ReceivedBy",
                table: "FuelDeliveries",
                type: "varchar(20)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "FuelDeliveries",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "FuelDeliveries",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AddColumn<string>(
                name: "CanceledBy",
                table: "FuelDeliveries",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CanceledDate",
                table: "FuelDeliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EditedBy",
                table: "FuelDeliveries",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EditedDate",
                table: "FuelDeliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GainOrLoss",
                table: "FuelDeliveries",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PlateNo",
                table: "FuelDeliveries",
                type: "varchar(20)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PostedBy",
                table: "FuelDeliveries",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PostedDate",
                table: "FuelDeliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StationCode",
                table: "FuelDeliveries",
                type: "varchar(3)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "StationPosCode",
                table: "FuelDeliveries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VoidedBy",
                table: "FuelDeliveries",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedDate",
                table: "FuelDeliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LubeDeliveryHeaders",
                columns: table => new
                {
                    LubeDeliveryHeaderId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CanceledBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    CanceledDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeliveryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DrNo = table.Column<string>(type: "varchar(10)", nullable: false),
                    EditedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    EditedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PoNo = table.Column<string>(type: "varchar(10)", nullable: false),
                    PostedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    PostedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SalesInvoice = table.Column<string>(type: "varchar(10)", nullable: false),
                    SupplierCode = table.Column<string>(type: "varchar(10)", nullable: false),
                    SupplierName = table.Column<string>(type: "varchar(200)", nullable: false),
                    VoidedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    VoidedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LubeDeliveryHeaders", x => x.LubeDeliveryHeaderId);
                });

            migrationBuilder.CreateTable(
                name: "LubeDeliveryDetails",
                columns: table => new
                {
                    LubeDeliveryDetailId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LubeDeliveryHeaderId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Description = table.Column<string>(type: "varchar(200)", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Unit = table.Column<string>(type: "varchar(10)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LubeDeliveryDetails", x => x.LubeDeliveryDetailId);
                    table.ForeignKey(
                        name: "FK_LubeDeliveryDetails_LubeDeliveryHeaders_LubeDeliveryHeaderId",
                        column: x => x.LubeDeliveryHeaderId,
                        principalTable: "LubeDeliveryHeaders",
                        principalColumn: "LubeDeliveryHeaderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LubeDeliveryDetails_LubeDeliveryHeaderId",
                table: "LubeDeliveryDetails",
                column: "LubeDeliveryHeaderId");
        }
    }
}
