using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddFirstAndSecondApprovalFieldInFilprideCOS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "approved_date",
                table: "filpride_customer_order_slips",
                newName: "second_approved_date");

            migrationBuilder.RenameColumn(
                name: "approved_by",
                table: "filpride_customer_order_slips",
                newName: "second_approved_by");

            migrationBuilder.AddColumn<string>(
                name: "first_approved_by",
                table: "filpride_customer_order_slips",
                type: "varchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "first_approved_date",
                table: "filpride_customer_order_slips",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "first_approved_by",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "first_approved_date",
                table: "filpride_customer_order_slips");

            migrationBuilder.RenameColumn(
                name: "second_approved_date",
                table: "filpride_customer_order_slips",
                newName: "approved_date");

            migrationBuilder.RenameColumn(
                name: "second_approved_by",
                table: "filpride_customer_order_slips",
                newName: "approved_by");
        }
    }
}
