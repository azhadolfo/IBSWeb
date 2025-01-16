using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddTableNamedFilprideMultipleCheckVoucherPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "filpride_multiple_check_voucher_payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    check_voucher_header_payment_id = table.Column<int>(type: "integer", nullable: false),
                    check_voucher_header_invoice_id = table.Column<int>(type: "integer", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_multiple_check_voucher_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_filpride_multiple_check_voucher_payments_filpride_check_vou",
                        column: x => x.check_voucher_header_invoice_id,
                        principalTable: "filpride_check_voucher_headers",
                        principalColumn: "check_voucher_header_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_multiple_check_voucher_payments_filpride_check_vou1",
                        column: x => x.check_voucher_header_payment_id,
                        principalTable: "filpride_check_voucher_headers",
                        principalColumn: "check_voucher_header_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_filpride_multiple_check_voucher_payments_check_voucher_head",
                table: "filpride_multiple_check_voucher_payments",
                column: "check_voucher_header_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_multiple_check_voucher_payments_check_voucher_head1",
                table: "filpride_multiple_check_voucher_payments",
                column: "check_voucher_header_payment_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "filpride_multiple_check_voucher_payments");
        }
    }
}
