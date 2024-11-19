using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovedByAndUploadByInMobilityCustomerModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "approved_by",
                table: "mobility_customer_order_slips",
                type: "varchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "approved_date",
                table: "mobility_customer_order_slips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "uploaded_by",
                table: "mobility_customer_order_slips",
                type: "varchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "uploaded_date",
                table: "mobility_customer_order_slips",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "approved_by",
                table: "mobility_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "approved_date",
                table: "mobility_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "uploaded_by",
                table: "mobility_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "uploaded_date",
                table: "mobility_customer_order_slips");
        }
    }
}
