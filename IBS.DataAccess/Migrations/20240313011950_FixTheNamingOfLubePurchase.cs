using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FixTheNamingOfLubePurchase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LubePurchaseDetails_LubePurchaseHeaders_LubeDeliveryHeaderId",
                table: "LubePurchaseDetails");

            migrationBuilder.RenameColumn(
                name: "LubePurchaseNo",
                table: "LubePurchaseHeaders",
                newName: "LubePurchaseHeaderNo");

            migrationBuilder.RenameColumn(
                name: "LubeDeliveryHeaderId",
                table: "LubePurchaseHeaders",
                newName: "LubePurchaseHeaderId");

            migrationBuilder.RenameColumn(
                name: "LubeDeliveryHeaderId",
                table: "LubePurchaseDetails",
                newName: "LubePurchaseHeaderId");

            migrationBuilder.RenameColumn(
                name: "LubeDeliveryDetailId",
                table: "LubePurchaseDetails",
                newName: "LubePurchaseDetailId");

            migrationBuilder.RenameIndex(
                name: "IX_LubePurchaseDetails_LubeDeliveryHeaderId",
                table: "LubePurchaseDetails",
                newName: "IX_LubePurchaseDetails_LubePurchaseHeaderId");

            migrationBuilder.AddColumn<string>(
                name: "LubePurchaseHeaderNo",
                table: "LubePurchaseDetails",
                type: "varchar(25)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_LubePurchaseDetails_LubePurchaseHeaders_LubePurchaseHeaderId",
                table: "LubePurchaseDetails",
                column: "LubePurchaseHeaderId",
                principalTable: "LubePurchaseHeaders",
                principalColumn: "LubePurchaseHeaderId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LubePurchaseDetails_LubePurchaseHeaders_LubePurchaseHeaderId",
                table: "LubePurchaseDetails");

            migrationBuilder.DropColumn(
                name: "LubePurchaseHeaderNo",
                table: "LubePurchaseDetails");

            migrationBuilder.RenameColumn(
                name: "LubePurchaseHeaderNo",
                table: "LubePurchaseHeaders",
                newName: "LubePurchaseNo");

            migrationBuilder.RenameColumn(
                name: "LubePurchaseHeaderId",
                table: "LubePurchaseHeaders",
                newName: "LubeDeliveryHeaderId");

            migrationBuilder.RenameColumn(
                name: "LubePurchaseHeaderId",
                table: "LubePurchaseDetails",
                newName: "LubeDeliveryHeaderId");

            migrationBuilder.RenameColumn(
                name: "LubePurchaseDetailId",
                table: "LubePurchaseDetails",
                newName: "LubeDeliveryDetailId");

            migrationBuilder.RenameIndex(
                name: "IX_LubePurchaseDetails_LubePurchaseHeaderId",
                table: "LubePurchaseDetails",
                newName: "IX_LubePurchaseDetails_LubeDeliveryHeaderId");

            migrationBuilder.AddForeignKey(
                name: "FK_LubePurchaseDetails_LubePurchaseHeaders_LubeDeliveryHeaderId",
                table: "LubePurchaseDetails",
                column: "LubeDeliveryHeaderId",
                principalTable: "LubePurchaseHeaders",
                principalColumn: "LubeDeliveryHeaderId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
