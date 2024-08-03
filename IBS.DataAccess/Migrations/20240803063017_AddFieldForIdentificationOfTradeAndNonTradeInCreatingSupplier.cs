using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldForIdentificationOfTradeAndNonTradeInCreatingSupplier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "branch",
                table: "filpride_suppliers",
                type: "varchar(10)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "filpride_suppliers",
                type: "varchar(20)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "default_expense_number",
                table: "filpride_suppliers",
                type: "varchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "trade_name",
                table: "filpride_suppliers",
                type: "varchar(255)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "withholding_tax_percent",
                table: "filpride_suppliers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "withholding_taxtitle",
                table: "filpride_suppliers",
                type: "varchar(100)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "branch",
                table: "filpride_suppliers");

            migrationBuilder.DropColumn(
                name: "category",
                table: "filpride_suppliers");

            migrationBuilder.DropColumn(
                name: "default_expense_number",
                table: "filpride_suppliers");

            migrationBuilder.DropColumn(
                name: "trade_name",
                table: "filpride_suppliers");

            migrationBuilder.DropColumn(
                name: "withholding_tax_percent",
                table: "filpride_suppliers");

            migrationBuilder.DropColumn(
                name: "withholding_taxtitle",
                table: "filpride_suppliers");
        }
    }
}
