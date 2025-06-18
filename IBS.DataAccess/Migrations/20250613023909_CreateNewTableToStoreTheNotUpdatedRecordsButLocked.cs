using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CreateNewTableToStoreTheNotUpdatedRecordsButLocked : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "filpride_purchase_locked_records_queues",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    locked_date = table.Column<DateOnly>(type: "date", nullable: false),
                    receiving_report_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_purchase_locked_records_queues", x => x.id);
                    table.ForeignKey(
                        name: "fk_filpride_purchase_locked_records_queues_filpride_receiving_",
                        column: x => x.receiving_report_id,
                        principalTable: "filpride_receiving_reports",
                        principalColumn: "receiving_report_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "filpride_sales_locked_records_queues",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    locked_date = table.Column<DateOnly>(type: "date", nullable: false),
                    delivery_receipt_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_sales_locked_records_queues", x => x.id);
                    table.ForeignKey(
                        name: "fk_filpride_sales_locked_records_queues_filpride_delivery_rece",
                        column: x => x.delivery_receipt_id,
                        principalTable: "filpride_delivery_receipts",
                        principalColumn: "delivery_receipt_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_filpride_purchase_locked_records_queues_locked_date",
                table: "filpride_purchase_locked_records_queues",
                column: "locked_date");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_purchase_locked_records_queues_receiving_report_id",
                table: "filpride_purchase_locked_records_queues",
                column: "receiving_report_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_sales_locked_records_queues_delivery_receipt_id",
                table: "filpride_sales_locked_records_queues",
                column: "delivery_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_sales_locked_records_queues_locked_date",
                table: "filpride_sales_locked_records_queues",
                column: "locked_date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "filpride_purchase_locked_records_queues");

            migrationBuilder.DropTable(
                name: "filpride_sales_locked_records_queues");
        }
    }
}
