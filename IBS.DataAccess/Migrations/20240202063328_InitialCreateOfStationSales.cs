using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateOfStationSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Fuels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Start = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    End = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "date", nullable: false),
                    CorpCode = table.Column<int>(type: "integer", nullable: false),
                    SiteCode = table.Column<int>(type: "integer", nullable: false),
                    Tank = table.Column<int>(type: "integer", nullable: false),
                    Pump = table.Column<int>(type: "integer", nullable: false),
                    Nozzle = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Day = table.Column<int>(type: "integer", nullable: false),
                    Transaction = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AmountDB = table.Column<decimal>(type: "numeric", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Calibration = table.Column<decimal>(type: "numeric", nullable: false),
                    Volume = table.Column<decimal>(type: "numeric", nullable: false),
                    ItemCode = table.Column<string>(type: "varchar(16)", nullable: false),
                    Particulars = table.Column<string>(type: "varchar(32)", nullable: false),
                    Opening = table.Column<long>(type: "bigint", nullable: false),
                    Closing = table.Column<long>(type: "bigint", nullable: false),
                    NozDown = table.Column<string>(type: "varchar(20)", nullable: false),
                    InTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OutTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Liters = table.Column<decimal>(type: "numeric", nullable: false),
                    CashierId = table.Column<string>(type: "varchar(10)", nullable: false),
                    CashierName = table.Column<string>(type: "varchar(20)", nullable: false),
                    Shift = table.Column<int>(type: "integer", nullable: false),
                    TransCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EditedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    EditedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                name: "Lubes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvoiceDate = table.Column<DateTime>(type: "date", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Day = table.Column<int>(type: "integer", nullable: false),
                    CorpCode = table.Column<int>(type: "integer", nullable: false),
                    SiteCode = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AmountDB = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Volume = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ItemCode = table.Column<string>(type: "varchar(16)", nullable: false),
                    Particulars = table.Column<string>(type: "varchar(32)", nullable: false),
                    CashierId = table.Column<string>(type: "varchar(10)", nullable: false),
                    CashierName = table.Column<string>(type: "varchar(20)", nullable: false),
                    Shift = table.Column<int>(type: "integer", nullable: false),
                    Transaction = table.Column<long>(type: "bigint", nullable: false),
                    DatetimeStamp = table.Column<string>(type: "varchar(200)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EditedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    EditedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                name: "SafeDrops",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvoiceDate = table.Column<DateTime>(type: "date", nullable: false),
                    BDate = table.Column<DateTime>(type: "date", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Day = table.Column<int>(type: "integer", nullable: false),
                    CorpCode = table.Column<int>(type: "integer", nullable: false),
                    SiteCode = table.Column<int>(type: "integer", nullable: false),
                    TransactionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateTimeStamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CashierId = table.Column<string>(type: "varchar(10)", nullable: false),
                    CashierName = table.Column<string>(type: "varchar(20)", nullable: false),
                    Shift = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EditedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    EditedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Fuels");

            migrationBuilder.DropTable(
                name: "Lubes");

            migrationBuilder.DropTable(
                name: "SafeDrops");
        }
    }
}
