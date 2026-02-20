using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovedByOnCVHeader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "approved_by",
                table: "filpride_check_voucher_headers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "approved_date",
                table: "filpride_check_voucher_headers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "approved_by",
                table: "filpride_check_voucher_headers");

            migrationBuilder.DropColumn(
                name: "approved_date",
                table: "filpride_check_voucher_headers");
        }
    }
}
