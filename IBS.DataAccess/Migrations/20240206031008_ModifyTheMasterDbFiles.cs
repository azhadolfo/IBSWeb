using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ModifyTheMasterDbFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TinNo",
                table: "Suppliers",
                newName: "SupplierTin");

            migrationBuilder.RenameColumn(
                name: "Terms",
                table: "Suppliers",
                newName: "SupplierTerms");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Suppliers",
                newName: "SupplierName");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "Suppliers",
                newName: "SupplierCode");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "Suppliers",
                newName: "SupplierAddress");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Suppliers",
                newName: "SupplierId");

            migrationBuilder.RenameColumn(
                name: "Unit",
                table: "Products",
                newName: "ProductUnit");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Products",
                newName: "ProductName");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "Products",
                newName: "ProductCode");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Products",
                newName: "ProductId");

            migrationBuilder.RenameColumn(
                name: "TinNo",
                table: "Customers",
                newName: "CustomerTin");

            migrationBuilder.RenameColumn(
                name: "Terms",
                table: "Customers",
                newName: "CustomerTerms");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Customers",
                newName: "CustomerName");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "Customers",
                newName: "CustomerCode");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "Customers",
                newName: "CustomerAddress");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Customers",
                newName: "CustomerId");

            migrationBuilder.RenameColumn(
                name: "TinNo",
                table: "Companies",
                newName: "CompanyTin");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Companies",
                newName: "CompanyName");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "Companies",
                newName: "CompanyCode");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "Companies",
                newName: "CompanyAddress");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Companies",
                newName: "CompanyId");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "ChartOfAccounts",
                newName: "AccountType");

            migrationBuilder.RenameColumn(
                name: "Number",
                table: "ChartOfAccounts",
                newName: "AccountNumber");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "ChartOfAccounts",
                newName: "AccountName");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "ChartOfAccounts",
                newName: "AccountCategory");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ChartOfAccounts",
                newName: "AccountId");

            migrationBuilder.CreateTable(
                name: "Stations",
                columns: table => new
                {
                    StationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PosCode = table.Column<int>(type: "integer", nullable: false),
                    StationCode = table.Column<string>(type: "varchar(5)", nullable: false),
                    StationName = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stations", x => x.StationId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChartOfAccounts_AccountName",
                table: "ChartOfAccounts",
                column: "AccountName");

            migrationBuilder.CreateIndex(
                name: "IX_ChartOfAccounts_AccountNumber",
                table: "ChartOfAccounts",
                column: "AccountNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Stations");

            migrationBuilder.DropIndex(
                name: "IX_ChartOfAccounts_AccountName",
                table: "ChartOfAccounts");

            migrationBuilder.DropIndex(
                name: "IX_ChartOfAccounts_AccountNumber",
                table: "ChartOfAccounts");

            migrationBuilder.RenameColumn(
                name: "SupplierTin",
                table: "Suppliers",
                newName: "TinNo");

            migrationBuilder.RenameColumn(
                name: "SupplierTerms",
                table: "Suppliers",
                newName: "Terms");

            migrationBuilder.RenameColumn(
                name: "SupplierName",
                table: "Suppliers",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "SupplierCode",
                table: "Suppliers",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "SupplierAddress",
                table: "Suppliers",
                newName: "Address");

            migrationBuilder.RenameColumn(
                name: "SupplierId",
                table: "Suppliers",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "ProductUnit",
                table: "Products",
                newName: "Unit");

            migrationBuilder.RenameColumn(
                name: "ProductName",
                table: "Products",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "ProductCode",
                table: "Products",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "Products",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "CustomerTin",
                table: "Customers",
                newName: "TinNo");

            migrationBuilder.RenameColumn(
                name: "CustomerTerms",
                table: "Customers",
                newName: "Terms");

            migrationBuilder.RenameColumn(
                name: "CustomerName",
                table: "Customers",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "CustomerCode",
                table: "Customers",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "CustomerAddress",
                table: "Customers",
                newName: "Address");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "Customers",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "CompanyTin",
                table: "Companies",
                newName: "TinNo");

            migrationBuilder.RenameColumn(
                name: "CompanyName",
                table: "Companies",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "CompanyCode",
                table: "Companies",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "CompanyAddress",
                table: "Companies",
                newName: "Address");

            migrationBuilder.RenameColumn(
                name: "CompanyId",
                table: "Companies",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "AccountType",
                table: "ChartOfAccounts",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "AccountNumber",
                table: "ChartOfAccounts",
                newName: "Number");

            migrationBuilder.RenameColumn(
                name: "AccountName",
                table: "ChartOfAccounts",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "AccountCategory",
                table: "ChartOfAccounts",
                newName: "Category");

            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "ChartOfAccounts",
                newName: "Id");
        }
    }
}
