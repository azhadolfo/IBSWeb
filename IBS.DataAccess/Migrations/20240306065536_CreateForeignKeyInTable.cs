using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CreateForeignKeyInTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Stations_StationId",
                table: "Inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_LubePurchaseDetails_LubePurchaseHeaders_LubeDeliveryHeaderId",
                table: "LubePurchaseDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesDetails_SalesHeaders_SalesHeaderId",
                table: "SalesDetails");

            migrationBuilder.DropIndex(
                name: "IX_SalesHeaders_SalesHeaderId",
                table: "SalesHeaders");

            migrationBuilder.DropIndex(
                name: "IX_Products_ProductId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_StationId",
                table: "Inventories");

            migrationBuilder.DropIndex(
                name: "IX_Customers_CustomerId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Companies_CompanyId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "StationId",
                table: "Inventories");

            migrationBuilder.AddForeignKey(
                name: "FK_LubePurchaseDetails_LubePurchaseHeaders_LubeDeliveryHeaderId",
                table: "LubePurchaseDetails",
                column: "LubeDeliveryHeaderId",
                principalTable: "LubePurchaseHeaders",
                principalColumn: "LubeDeliveryHeaderId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesDetails_SalesHeaders_SalesHeaderId",
                table: "SalesDetails",
                column: "SalesHeaderId",
                principalTable: "SalesHeaders",
                principalColumn: "SalesHeaderId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LubePurchaseDetails_LubePurchaseHeaders_LubeDeliveryHeaderId",
                table: "LubePurchaseDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesDetails_SalesHeaders_SalesHeaderId",
                table: "SalesDetails");

            migrationBuilder.AddColumn<int>(
                name: "StationId",
                table: "Inventories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SalesHeaders_SalesHeaderId",
                table: "SalesHeaders",
                column: "SalesHeaderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductId",
                table: "Products",
                column: "ProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_StationId",
                table: "Inventories",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerId",
                table: "Customers",
                column: "CustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CompanyId",
                table: "Companies",
                column: "CompanyId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Stations_StationId",
                table: "Inventories",
                column: "StationId",
                principalTable: "Stations",
                principalColumn: "StationId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LubePurchaseDetails_LubePurchaseHeaders_LubeDeliveryHeaderId",
                table: "LubePurchaseDetails",
                column: "LubeDeliveryHeaderId",
                principalTable: "LubePurchaseHeaders",
                principalColumn: "LubeDeliveryHeaderId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesDetails_SalesHeaders_SalesHeaderId",
                table: "SalesDetails",
                column: "SalesHeaderId",
                principalTable: "SalesHeaders",
                principalColumn: "SalesHeaderId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
