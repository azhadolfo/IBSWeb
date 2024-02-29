using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CreateTheLubeDeliveryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LubeDeliveryHeaders",
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
                    table.PrimaryKey("PK_LubeDeliveryHeaders", x => x.LubeDeliveryHeaderId);
                });

            migrationBuilder.CreateTable(
                name: "LubeDeliveryDetails",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LubeDeliveryDetails");

            migrationBuilder.DropTable(
                name: "LubeDeliveryHeaders");
        }
    }
}
