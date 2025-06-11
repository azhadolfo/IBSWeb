using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddNewtableForMobilityCollectionReceipt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mobility_collection_receipts",
                columns: table => new
                {
                    collection_receipt_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    collection_receipt_no = table.Column<string>(type: "varchar(12)", nullable: true),
                    service_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    sv_no = table.Column<string>(type: "varchar(12)", nullable: true),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    series_number = table.Column<long>(type: "bigint", nullable: false),
                    reference_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    remarks = table.Column<string>(type: "varchar(100)", nullable: true),
                    cash_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    check_date = table.Column<string>(type: "text", nullable: true),
                    check_no = table.Column<string>(type: "varchar(50)", nullable: true),
                    check_bank = table.Column<string>(type: "varchar(50)", nullable: true),
                    check_branch = table.Column<string>(type: "varchar(50)", nullable: true),
                    check_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    manager_check_date = table.Column<DateOnly>(type: "date", nullable: true),
                    manager_check_no = table.Column<string>(type: "varchar(50)", nullable: true),
                    manager_check_bank = table.Column<string>(type: "varchar(50)", nullable: true),
                    manager_check_branch = table.Column<string>(type: "varchar(50)", nullable: true),
                    manager_check_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ewt = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    wvat = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_certificate_upload = table.Column<bool>(type: "boolean", nullable: false),
                    f2306file_path = table.Column<string>(type: "varchar(200)", nullable: true),
                    f2306file_name = table.Column<string>(type: "text", nullable: true),
                    f2307file_path = table.Column<string>(type: "varchar(200)", nullable: true),
                    f2307file_name = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_mobility_collection_receipts", x => x.collection_receipt_id);
                    table.ForeignKey(
                        name: "fk_mobility_collection_receipts_mobility_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "mobility_customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mobility_collection_receipts_mobility_service_invoices_serv",
                        column: x => x.service_invoice_id,
                        principalTable: "mobility_service_invoices",
                        principalColumn: "service_invoice_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mobility_collection_receipts_customer_id",
                table: "mobility_collection_receipts",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_collection_receipts_service_invoice_id",
                table: "mobility_collection_receipts",
                column: "service_invoice_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mobility_collection_receipts");
        }
    }
}
