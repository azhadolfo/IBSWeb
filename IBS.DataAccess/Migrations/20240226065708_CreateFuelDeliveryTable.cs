using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CreateFuelDeliveryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FuelDeliveries",
                columns: table => new
                {
                    FuelDeliveryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    QuantityBefore = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    QuantityAfter = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ShouldBe = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    GainOrLoss = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReceivedBy = table.Column<string>(type: "varchar(20)", nullable: false),
                    StationCode = table.Column<string>(type: "varchar(3)", nullable: false),
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
                    table.PrimaryKey("PK_FuelDeliveries", x => x.FuelDeliveryId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FuelDeliveries");
        }
    }
}
