using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTheOldFilprideReceivingReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "filpride_receiving_reports");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "filpride_receiving_reports",
                columns: table => new
                {
                    receiving_report_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    delivery_receipt_id = table.Column<int>(type: "integer", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    gain_or_loss = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    net_of_tax_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    net_of_vat_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    other_reference = table.Column<string>(type: "varchar(100)", nullable: false),
                    paid_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    quantity_delivered = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    quantity_received = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    receiving_report_no = table.Column<string>(type: "varchar(12)", nullable: false),
                    remarks = table.Column<string>(type: "varchar(200)", nullable: false),
                    supplier_dr_date = table.Column<DateOnly>(type: "date", nullable: true),
                    supplier_dr_no = table.Column<string>(type: "varchar(50)", nullable: true),
                    supplier_si_date = table.Column<DateOnly>(type: "date", nullable: true),
                    supplier_si_no = table.Column<string>(type: "varchar(50)", nullable: true),
                    total_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_freight = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    withdrawal_certificate = table.Column<string>(type: "varchar(50)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_receiving_reports", x => x.receiving_report_id);
                    table.ForeignKey(
                        name: "fk_filpride_receiving_reports_filpride_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "filpride_customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_receiving_reports_filpride_delivery_receipts_deliv",
                        column: x => x.delivery_receipt_id,
                        principalTable: "filpride_delivery_receipts",
                        principalColumn: "delivery_receipt_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_filpride_receiving_reports_customer_id",
                table: "filpride_receiving_reports",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_receiving_reports_date",
                table: "filpride_receiving_reports",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_receiving_reports_delivery_receipt_id",
                table: "filpride_receiving_reports",
                column: "delivery_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_receiving_reports_receiving_report_no",
                table: "filpride_receiving_reports",
                column: "receiving_report_no",
                unique: true);
        }
    }
}
