using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPOSalesFieldInSalesHeader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "POSalesAmount",
                table: "SalesHeaders",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "cust",
                table: "Lubes",
                type: "varchar(20)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "plateno",
                table: "Lubes",
                type: "varchar(20)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pono",
                table: "Lubes",
                type: "varchar(20)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "POSalesAmount",
                table: "SalesHeaders");

            migrationBuilder.DropColumn(
                name: "cust",
                table: "Lubes");

            migrationBuilder.DropColumn(
                name: "plateno",
                table: "Lubes");

            migrationBuilder.DropColumn(
                name: "pono",
                table: "Lubes");
        }
    }
}
