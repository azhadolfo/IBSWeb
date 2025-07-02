using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCncApprovalInCos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cnc_approved_by",
                table: "filpride_customer_order_slips",
                type: "varchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "cnc_approved_date",
                table: "filpride_customer_order_slips",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cnc_approved_by",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "cnc_approved_date",
                table: "filpride_customer_order_slips");
        }
    }
}
