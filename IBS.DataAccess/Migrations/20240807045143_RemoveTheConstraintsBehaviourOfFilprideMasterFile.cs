using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTheConstraintsBehaviourOfFilprideMasterFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_haulers_hauler_code",
                table: "haulers");

            migrationBuilder.DropIndex(
                name: "ix_haulers_hauler_name",
                table: "haulers");

            migrationBuilder.DropIndex(
                name: "ix_filpride_suppliers_supplier_code",
                table: "filpride_suppliers");

            migrationBuilder.DropIndex(
                name: "ix_filpride_suppliers_supplier_name",
                table: "filpride_suppliers");

            migrationBuilder.DropIndex(
                name: "ix_filpride_customers_customer_code",
                table: "filpride_customers");

            migrationBuilder.CreateIndex(
                name: "ix_haulers_hauler_code",
                table: "haulers",
                column: "hauler_code");

            migrationBuilder.CreateIndex(
                name: "ix_haulers_hauler_name",
                table: "haulers",
                column: "hauler_name");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_suppliers_supplier_code",
                table: "filpride_suppliers",
                column: "supplier_code");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_suppliers_supplier_name",
                table: "filpride_suppliers",
                column: "supplier_name");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customers_customer_code",
                table: "filpride_customers",
                column: "customer_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_haulers_hauler_code",
                table: "haulers");

            migrationBuilder.DropIndex(
                name: "ix_haulers_hauler_name",
                table: "haulers");

            migrationBuilder.DropIndex(
                name: "ix_filpride_suppliers_supplier_code",
                table: "filpride_suppliers");

            migrationBuilder.DropIndex(
                name: "ix_filpride_suppliers_supplier_name",
                table: "filpride_suppliers");

            migrationBuilder.DropIndex(
                name: "ix_filpride_customers_customer_code",
                table: "filpride_customers");

            migrationBuilder.CreateIndex(
                name: "ix_haulers_hauler_code",
                table: "haulers",
                column: "hauler_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_haulers_hauler_name",
                table: "haulers",
                column: "hauler_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_suppliers_supplier_code",
                table: "filpride_suppliers",
                column: "supplier_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_suppliers_supplier_name",
                table: "filpride_suppliers",
                column: "supplier_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customers_customer_code",
                table: "filpride_customers",
                column: "customer_code",
                unique: true);
        }
    }
}
