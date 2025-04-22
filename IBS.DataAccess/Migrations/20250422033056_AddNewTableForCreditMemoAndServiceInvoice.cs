using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddNewTableForCreditMemoAndServiceInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mobility_service_invoices",
                columns: table => new
                {
                    service_invoice_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_invoice_no = table.Column<string>(type: "varchar(12)", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    customer_address = table.Column<string>(type: "text", nullable: false),
                    customer_tin = table.Column<string>(type: "text", nullable: false),
                    service_id = table.Column<int>(type: "integer", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    period = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    discount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    current_and_previous_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unearned_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    payment_status = table.Column<string>(type: "varchar(20)", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    instructions = table.Column<string>(type: "varchar(200)", nullable: true),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    station_code = table.Column<string>(type: "varchar(3)", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancellation_remarks = table.Column<string>(type: "varchar(255)", nullable: true),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_service_invoices", x => x.service_invoice_id);
                    table.ForeignKey(
                        name: "fk_mobility_service_invoices_mobility_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "mobility_customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mobility_service_invoices_mobility_services_service_id",
                        column: x => x.service_id,
                        principalTable: "mobility_services",
                        principalColumn: "service_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mobility_service_invoices_mobility_stations_station_code",
                        column: x => x.station_code,
                        principalTable: "mobility_stations",
                        principalColumn: "station_code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mobility_credit_memos",
                columns: table => new
                {
                    credit_memo_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    credit_memo_no = table.Column<string>(type: "varchar(12)", nullable: true),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    service_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    description = table.Column<string>(type: "text", nullable: false),
                    credit_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    remarks = table.Column<string>(type: "text", nullable: true),
                    period = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    current_and_previous_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unearned_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    station_code = table.Column<string>(type: "text", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancellation_remarks = table.Column<string>(type: "varchar(255)", nullable: true),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_credit_memos", x => x.credit_memo_id);
                    table.ForeignKey(
                        name: "fk_mobility_credit_memos_mobility_service_invoices_service_inv",
                        column: x => x.service_invoice_id,
                        principalTable: "mobility_service_invoices",
                        principalColumn: "service_invoice_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mobility_credit_memos_service_invoice_id",
                table: "mobility_credit_memos",
                column: "service_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_service_invoices_customer_id",
                table: "mobility_service_invoices",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_service_invoices_service_id",
                table: "mobility_service_invoices",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_service_invoices_station_code",
                table: "mobility_service_invoices",
                column: "station_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mobility_credit_memos");

            migrationBuilder.DropTable(
                name: "mobility_service_invoices");
        }
    }
}
