using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddedCommisioneeAndCommissioneeRateToCustomerModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "commission_rate",
                table: "filpride_customers",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "commissionee_id",
                table: "filpride_customers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customers_commissionee_id",
                table: "filpride_customers",
                column: "commissionee_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_customers_filpride_suppliers_commissionee_id",
                table: "filpride_customers",
                column: "commissionee_id",
                principalTable: "filpride_suppliers",
                principalColumn: "supplier_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_customers_filpride_suppliers_commissionee_id",
                table: "filpride_customers");

            migrationBuilder.DropIndex(
                name: "ix_filpride_customers_commissionee_id",
                table: "filpride_customers");

            migrationBuilder.DropColumn(
                name: "commission_rate",
                table: "filpride_customers");

            migrationBuilder.DropColumn(
                name: "commissionee_id",
                table: "filpride_customers");
        }
    }
}
