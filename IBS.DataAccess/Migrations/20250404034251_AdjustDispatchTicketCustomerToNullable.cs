﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AdjustDispatchTicketCustomerToNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_customers_customer_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.AlterColumn<int>(
                name: "customer_id",
                table: "mmsi_dispatch_tickets",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_customers_customer_id",
                table: "mmsi_dispatch_tickets",
                column: "customer_id",
                principalTable: "mmsi_customers",
                principalColumn: "mmsi_customer_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_customers_customer_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.AlterColumn<int>(
                name: "customer_id",
                table: "mmsi_dispatch_tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_customers_customer_id",
                table: "mmsi_dispatch_tickets",
                column: "customer_id",
                principalTable: "mmsi_customers",
                principalColumn: "mmsi_customer_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
