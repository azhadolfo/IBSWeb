using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSomeCalculatedValuesInFilpride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ewt_amount",
                table: "filpride_receiving_reports");

            migrationBuilder.DropColumn(
                name: "net_amount",
                table: "filpride_receiving_reports");

            migrationBuilder.DropColumn(
                name: "net_amount_of_ewt",
                table: "filpride_receiving_reports");

            migrationBuilder.DropColumn(
                name: "vat_amount",
                table: "filpride_receiving_reports");

            migrationBuilder.DropColumn(
                name: "total_sales",
                table: "filpride_credit_memos");

            migrationBuilder.DropColumn(
                name: "vat_amount",
                table: "filpride_credit_memos");

            migrationBuilder.DropColumn(
                name: "vatable_sales",
                table: "filpride_credit_memos");

            migrationBuilder.DropColumn(
                name: "with_holding_tax_amount",
                table: "filpride_credit_memos");

            migrationBuilder.DropColumn(
                name: "with_holding_vat_amount",
                table: "filpride_credit_memos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ewt_amount",
                table: "filpride_receiving_reports",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "net_amount",
                table: "filpride_receiving_reports",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "net_amount_of_ewt",
                table: "filpride_receiving_reports",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "vat_amount",
                table: "filpride_receiving_reports",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_sales",
                table: "filpride_credit_memos",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "vat_amount",
                table: "filpride_credit_memos",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "vatable_sales",
                table: "filpride_credit_memos",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "with_holding_tax_amount",
                table: "filpride_credit_memos",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "with_holding_vat_amount",
                table: "filpride_credit_memos",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
