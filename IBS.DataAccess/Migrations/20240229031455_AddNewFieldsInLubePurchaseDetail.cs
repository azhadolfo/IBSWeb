using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddNewFieldsInLubePurchaseDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "LubePurchaseDetails");

            migrationBuilder.AddColumn<int>(
                name: "Piece",
                table: "LubePurchaseDetails",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProductCode",
                table: "LubePurchaseDetails",
                type: "varchar(10)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Piece",
                table: "LubePurchaseDetails");

            migrationBuilder.DropColumn(
                name: "ProductCode",
                table: "LubePurchaseDetails");

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "LubePurchaseDetails",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
