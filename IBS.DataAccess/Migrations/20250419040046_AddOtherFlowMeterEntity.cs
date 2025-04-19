using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddOtherFlowMeterEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mobility_fms_calibrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_code = table.Column<string>(type: "text", nullable: false),
                    shift_record_id = table.Column<string>(type: "text", nullable: false),
                    pump_number = table.Column<int>(type: "integer", nullable: false),
                    product_code = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    shift_date = table.Column<DateOnly>(type: "date", nullable: false),
                    shift_number = table.Column<int>(type: "integer", nullable: false),
                    page_number = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_fms_calibrations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_fms_cashier_shifts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_code = table.Column<string>(type: "text", nullable: false),
                    shift_record_id = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    employee_number = table.Column<string>(type: "text", nullable: false),
                    shift_number = table.Column<int>(type: "integer", nullable: false),
                    page_number = table.Column<int>(type: "integer", nullable: false),
                    time_in = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    time_out = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    next_day = table.Column<bool>(type: "boolean", nullable: false),
                    cash_on_hand = table.Column<decimal>(type: "numeric", nullable: false),
                    biodiesel_price = table.Column<decimal>(type: "numeric", nullable: false),
                    econogas_price = table.Column<decimal>(type: "numeric", nullable: false),
                    envirogas_price = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_fms_cashier_shifts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mobility_fms_lube_sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_code = table.Column<string>(type: "text", nullable: false),
                    shift_record_id = table.Column<string>(type: "text", nullable: false),
                    product_code = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    actual_price = table.Column<decimal>(type: "numeric", nullable: false),
                    cost = table.Column<decimal>(type: "numeric", nullable: false),
                    shift_date = table.Column<DateOnly>(type: "date", nullable: false),
                    shift_number = table.Column<int>(type: "integer", nullable: false),
                    page_number = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_fms_lube_sales", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fms_calibrations_shift_record_id",
                table: "mobility_fms_calibrations",
                column: "shift_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fms_calibrations_station_code",
                table: "mobility_fms_calibrations",
                column: "station_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fms_cashier_shifts_shift_record_id",
                table: "mobility_fms_cashier_shifts",
                column: "shift_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fms_cashier_shifts_station_code",
                table: "mobility_fms_cashier_shifts",
                column: "station_code");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fms_lube_sales_shift_record_id",
                table: "mobility_fms_lube_sales",
                column: "shift_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fms_lube_sales_station_code",
                table: "mobility_fms_lube_sales",
                column: "station_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mobility_fms_calibrations");

            migrationBuilder.DropTable(
                name: "mobility_fms_cashier_shifts");

            migrationBuilder.DropTable(
                name: "mobility_fms_lube_sales");
        }
    }
}
