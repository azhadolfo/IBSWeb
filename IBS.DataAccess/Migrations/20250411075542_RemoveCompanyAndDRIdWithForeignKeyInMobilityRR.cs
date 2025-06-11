using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCompanyAndDRIdWithForeignKeyInMobilityRR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mobility_receiving_reports_filpride_delivery_receipts_deliv",
                table: "mobility_receiving_reports");

            migrationBuilder.DropIndex(
                name: "ix_mobility_receiving_reports_delivery_receipt_id",
                table: "mobility_receiving_reports");

            migrationBuilder.DropColumn(
                name: "company",
                table: "mobility_receiving_reports");

            migrationBuilder.DropColumn(
                name: "delivery_receipt_id",
                table: "mobility_receiving_reports");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "mobility_receiving_reports",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "delivery_receipt_id",
                table: "mobility_receiving_reports",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_mobility_receiving_reports_delivery_receipt_id",
                table: "mobility_receiving_reports",
                column: "delivery_receipt_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mobility_receiving_reports_filpride_delivery_receipts_deliv",
                table: "mobility_receiving_reports",
                column: "delivery_receipt_id",
                principalTable: "filpride_delivery_receipts",
                principalColumn: "delivery_receipt_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
