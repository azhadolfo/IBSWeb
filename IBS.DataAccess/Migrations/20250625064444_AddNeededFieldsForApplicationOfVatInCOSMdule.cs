using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddNeededFieldsForApplicationOfVatInCOSMdule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "available_credit_limit",
                table: "filpride_customer_order_slips",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "business_style",
                table: "filpride_customer_order_slips",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "commissionee_name",
                table: "filpride_customer_order_slips",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "credit_balance",
                table: "filpride_customer_order_slips",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "customer_name",
                table: "filpride_customer_order_slips",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "depot",
                table: "filpride_customer_order_slips",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "has_ewt",
                table: "filpride_customer_order_slips",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_wvat",
                table: "filpride_customer_order_slips",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "product_name",
                table: "filpride_customer_order_slips",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "vat_type",
                table: "filpride_customer_order_slips",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "available_credit_limit",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "business_style",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "commissionee_name",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "credit_balance",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "customer_name",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "depot",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "has_ewt",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "has_wvat",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "product_name",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "vat_type",
                table: "filpride_customer_order_slips");
        }
    }
}
