using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldsForApplicationOfVatInServiceInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "customer_business_type",
                table: "filpride_service_invoices",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "customer_name",
                table: "filpride_service_invoices",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "has_ewt",
                table: "filpride_service_invoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_wvat",
                table: "filpride_service_invoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "service_name",
                table: "filpride_service_invoices",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "vat_type",
                table: "filpride_service_invoices",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "customer_business_type",
                table: "filpride_service_invoices");

            migrationBuilder.DropColumn(
                name: "customer_name",
                table: "filpride_service_invoices");

            migrationBuilder.DropColumn(
                name: "has_ewt",
                table: "filpride_service_invoices");

            migrationBuilder.DropColumn(
                name: "has_wvat",
                table: "filpride_service_invoices");

            migrationBuilder.DropColumn(
                name: "service_name",
                table: "filpride_service_invoices");

            migrationBuilder.DropColumn(
                name: "vat_type",
                table: "filpride_service_invoices");
        }
    }
}
