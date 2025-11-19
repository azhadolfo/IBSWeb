using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeyOfFieldTermsInPurchaseOrderModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_filpride_purchase_orders_terms",
                table: "filpride_purchase_orders",
                column: "terms");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_purchase_orders_filpride_terms_terms",
                table: "filpride_purchase_orders",
                column: "terms",
                principalTable: "filpride_terms",
                principalColumn: "terms_code",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_purchase_orders_filpride_terms_terms",
                table: "filpride_purchase_orders");

            migrationBuilder.DropIndex(
                name: "ix_filpride_purchase_orders_terms",
                table: "filpride_purchase_orders");
        }
    }
}
