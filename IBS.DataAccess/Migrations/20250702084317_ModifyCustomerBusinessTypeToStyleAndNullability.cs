using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ModifyCustomerBusinessTypeToStyleAndNullability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "customer_business_type",
                table: "filpride_service_invoices");

            migrationBuilder.AddColumn<string>(
                name: "customer_business_style",
                table: "filpride_service_invoices",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "customer_business_style",
                table: "filpride_service_invoices");

            migrationBuilder.AddColumn<string>(
                name: "customer_business_type",
                table: "filpride_service_invoices",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
