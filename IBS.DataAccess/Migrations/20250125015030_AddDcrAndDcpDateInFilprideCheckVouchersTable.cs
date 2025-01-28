using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddDcrAndDcpDateInFilprideCheckVouchersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "dcp_date",
                table: "filpride_check_voucher_headers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "dcr_date",
                table: "filpride_check_voucher_headers",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "dcp_date",
                table: "filpride_check_voucher_headers");

            migrationBuilder.DropColumn(
                name: "dcr_date",
                table: "filpride_check_voucher_headers");
        }
    }
}
