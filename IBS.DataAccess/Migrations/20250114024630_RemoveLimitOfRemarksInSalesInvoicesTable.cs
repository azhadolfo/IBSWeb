using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLimitOfRemarksInSalesInvoicesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "remarks",
                table: "filpride_sales_invoices",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "remarks",
                table: "filpride_sales_invoices",
                type: "varchar(100)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
