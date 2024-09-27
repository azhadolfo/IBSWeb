using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveInvoiceNoInTheFilprideDrTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_filpride_delivery_receipts_invoice_no",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropColumn(
                name: "invoice_no",
                table: "filpride_delivery_receipts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "invoice_no",
                table: "filpride_delivery_receipts",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_receipts_invoice_no",
                table: "filpride_delivery_receipts",
                column: "invoice_no",
                unique: true);
        }
    }
}
