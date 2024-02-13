using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CompanyCode",
                table: "Companies",
                type: "varchar(3)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(3)");

            migrationBuilder.CreateIndex(
                name: "IX_SafeDrops_xONAME",
                table: "SafeDrops",
                column: "xONAME");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductCode",
                table: "Products",
                column: "ProductCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductId",
                table: "Products",
                column: "ProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductName",
                table: "Products",
                column: "ProductName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lubes_Cashier",
                table: "Lubes",
                column: "Cashier");

            migrationBuilder.CreateIndex(
                name: "IX_Fuels_xONAME",
                table: "Fuels",
                column: "xONAME");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerName",
                table: "Customers",
                column: "CustomerName");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CompanyCode",
                table: "Companies",
                column: "CompanyCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CompanyId",
                table: "Companies",
                column: "CompanyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CompanyName",
                table: "Companies",
                column: "CompanyName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SafeDrops_xONAME",
                table: "SafeDrops");

            migrationBuilder.DropIndex(
                name: "IX_Products_ProductCode",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_ProductId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_ProductName",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Lubes_Cashier",
                table: "Lubes");

            migrationBuilder.DropIndex(
                name: "IX_Fuels_xONAME",
                table: "Fuels");

            migrationBuilder.DropIndex(
                name: "IX_Customers_CustomerName",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Companies_CompanyCode",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_CompanyId",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_CompanyName",
                table: "Companies");

            migrationBuilder.AlterColumn<string>(
                name: "CompanyCode",
                table: "Companies",
                type: "varchar(3)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(3)",
                oldNullable: true);
        }
    }
}
