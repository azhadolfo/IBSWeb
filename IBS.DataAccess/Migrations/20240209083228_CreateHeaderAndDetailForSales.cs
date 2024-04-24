using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CreateHeaderAndDetailForSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesHeaders",
                columns: table => new
                {
                    SalesHeaderId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SalesNo = table.Column<string>(type: "varchar(25)", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    Cashier = table.Column<string>(type: "varchar(20)", nullable: false),
                    Shift = table.Column<int>(type: "integer", nullable: false),
                    SalesDropTotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LubesTotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_SalesHeaders", x => x.SalesHeaderId);
                });

            migrationBuilder.CreateTable(
                name: "SalesDetails",
                columns: table => new
                {
                    SalesDetailId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SalesHeaderId = table.Column<int>(type: "integer", nullable: false),
                    SalesNo = table.Column<string>(type: "varchar(25)", nullable: false),
                    Closing = table.Column<double>(type: "double precision", nullable: false),
                    Opening = table.Column<double>(type: "double precision", nullable: false),
                    Liters = table.Column<double>(type: "double precision", nullable: false),
                    Calibration = table.Column<int>(type: "integer", nullable: false),
                    LitersSold = table.Column<double>(type: "double precision", nullable: false),
                    TransactionCount = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Sale = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesDetails", x => x.SalesDetailId);
                    table.ForeignKey(
                        name: "FK_SalesDetails_SalesHeaders_SalesHeaderId",
                        column: x => x.SalesHeaderId,
                        principalTable: "SalesHeaders",
                        principalColumn: "SalesHeaderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesDetails_SalesHeaderId",
                table: "SalesDetails",
                column: "SalesHeaderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesDetails");

            migrationBuilder.DropTable(
                name: "SalesHeaders");
        }
    }
}
