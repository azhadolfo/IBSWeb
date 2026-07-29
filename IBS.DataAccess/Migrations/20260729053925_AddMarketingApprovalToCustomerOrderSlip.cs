using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketingApprovalToCustomerOrderSlip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "marketing_approved_by",
                table: "filpride_customer_order_slips",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "marketing_approved_date",
                table: "filpride_customer_order_slips",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "marketing_approved_by",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "marketing_approved_date",
                table: "filpride_customer_order_slips");
        }
    }
}
