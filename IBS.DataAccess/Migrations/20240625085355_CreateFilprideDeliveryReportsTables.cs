﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CreateFilprideDeliveryReportsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "filpride_delivery_reports",
                columns: table => new
                {
                    delivery_report_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    delivery_report_no = table.Column<string>(type: "varchar(12)", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    invoice_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    customer_order_slip_id = table.Column<int>(type: "integer", nullable: false),
                    hauler_id = table.Column<int>(type: "integer", nullable: false),
                    driver = table.Column<string>(type: "varchar(50)", nullable: false),
                    truck_and_plate_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    authority_to_load_no = table.Column<string>(type: "varchar(20)", nullable: false),
                    remarks = table.Column<string>(type: "varchar(200)", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    net_of_vat_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
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
                    table.PrimaryKey("pk_filpride_delivery_reports", x => x.delivery_report_id);
                    table.ForeignKey(
                        name: "fk_filpride_delivery_reports_filpride_customer_order_slips_cus",
                        column: x => x.customer_order_slip_id,
                        principalTable: "filpride_customer_order_slips",
                        principalColumn: "customer_order_slip_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_delivery_reports_haulers_hauler_id",
                        column: x => x.hauler_id,
                        principalTable: "haulers",
                        principalColumn: "hauler_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_reports_customer_order_slip_id",
                table: "filpride_delivery_reports",
                column: "customer_order_slip_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_reports_date",
                table: "filpride_delivery_reports",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_reports_delivery_report_no",
                table: "filpride_delivery_reports",
                column: "delivery_report_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_reports_hauler_id",
                table: "filpride_delivery_reports",
                column: "hauler_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_reports_invoice_no",
                table: "filpride_delivery_reports",
                column: "invoice_no",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "filpride_delivery_reports");
        }
    }
}
