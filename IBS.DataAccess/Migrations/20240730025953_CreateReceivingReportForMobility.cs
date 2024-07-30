using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CreateReceivingReportForMobility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mobility_receiving_reports",
                columns: table => new
                {
                    receiving_report_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    receiving_report_no = table.Column<string>(type: "varchar(15)", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    driver = table.Column<string>(type: "varchar(50)", nullable: false),
                    plate_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    remarks = table.Column<string>(type: "varchar(200)", nullable: false),
                    received_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    station_code = table.Column<string>(type: "varchar(3)", nullable: false),
                    delivery_receipt_id = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_receiving_reports", x => x.receiving_report_id);
                    table.ForeignKey(
                        name: "fk_mobility_receiving_reports_filpride_delivery_receipts_deliv",
                        column: x => x.delivery_receipt_id,
                        principalTable: "filpride_delivery_receipts",
                        principalColumn: "delivery_receipt_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mobility_receiving_reports_delivery_receipt_id",
                table: "mobility_receiving_reports",
                column: "delivery_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_receiving_reports_receiving_report_no",
                table: "mobility_receiving_reports",
                column: "receiving_report_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mobility_receiving_reports_station_code",
                table: "mobility_receiving_reports",
                column: "station_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mobility_receiving_reports");
        }
    }
}
