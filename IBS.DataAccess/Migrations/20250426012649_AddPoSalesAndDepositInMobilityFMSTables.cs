using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPoSalesAndDepositInMobilityFMSTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mobility_fms_deposits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_code = table.Column<string>(type: "text", nullable: false),
                    shift_record_id = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    account_number = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    shift_date = table.Column<DateOnly>(type: "date", nullable: false),
                    shift_number = table.Column<int>(type: "integer", nullable: false),
                    page_number = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_fms_deposits", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_fms_po_sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_code = table.Column<string>(type: "text", nullable: false),
                    shift_record_id = table.Column<string>(type: "text", nullable: false),
                    product_code = table.Column<string>(type: "text", nullable: false),
                    customer_code = table.Column<string>(type: "text", nullable: false),
                    trip_ticket = table.Column<string>(type: "text", nullable: false),
                    dr_number = table.Column<string>(type: "text", nullable: false),
                    driver = table.Column<string>(type: "text", nullable: false),
                    plate_no = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    contract_price = table.Column<decimal>(type: "numeric", nullable: false),
                    time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    shift_date = table.Column<DateOnly>(type: "date", nullable: false),
                    shift_number = table.Column<int>(type: "integer", nullable: false),
                    page_number = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_fms_po_sales", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mobility_fms_deposits");

            migrationBuilder.DropTable(
                name: "mobility_fms_po_sales");
        }
    }
}
