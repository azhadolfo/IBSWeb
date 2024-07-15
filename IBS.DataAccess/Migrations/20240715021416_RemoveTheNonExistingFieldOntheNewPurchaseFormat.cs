using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTheNonExistingFieldOntheNewPurchaseFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_mobility_lube_deliveries_dtllink",
                table: "mobility_lube_deliveries");

            migrationBuilder.DropIndex(
                name: "ix_mobility_lube_deliveries_shiftrecid",
                table: "mobility_lube_deliveries");

            migrationBuilder.DropIndex(
                name: "ix_mobility_fuel_deliveries_shiftrecid",
                table: "mobility_fuel_deliveries");

            migrationBuilder.DropColumn(
                name: "detail_link",
                table: "mobility_lube_purchase_headers");

            migrationBuilder.DropColumn(
                name: "shift_rec_id",
                table: "mobility_lube_purchase_headers");

            migrationBuilder.DropColumn(
                name: "dtllink",
                table: "mobility_lube_deliveries");

            migrationBuilder.DropColumn(
                name: "shiftrecid",
                table: "mobility_lube_deliveries");

            migrationBuilder.DropColumn(
                name: "shift_rec_id",
                table: "mobility_fuel_purchase");

            migrationBuilder.DropColumn(
                name: "shiftrecid",
                table: "mobility_fuel_deliveries");

            migrationBuilder.RenameColumn(
                name: "delivery_date",
                table: "mobility_lube_purchase_headers",
                newName: "shift_date");

            migrationBuilder.RenameColumn(
                name: "deliverydate",
                table: "mobility_lube_deliveries",
                newName: "shiftdate");

            migrationBuilder.RenameColumn(
                name: "delivery_date",
                table: "mobility_fuel_purchase",
                newName: "shift_date");

            migrationBuilder.RenameColumn(
                name: "deliverydate",
                table: "mobility_fuel_deliveries",
                newName: "shiftdate");

            migrationBuilder.AddColumn<int>(
                name: "page_number",
                table: "mobility_lube_purchase_headers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "cost",
                table: "mobility_lube_deliveries",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "freight",
                table: "mobility_lube_deliveries",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "pagenumber",
                table: "mobility_lube_deliveries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "srp",
                table: "mobility_lube_deliveries",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "page_number",
                table: "mobility_fuel_purchase",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "pagenumber",
                table: "mobility_fuel_deliveries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_mobility_lube_deliveries_pagenumber",
                table: "mobility_lube_deliveries",
                column: "pagenumber");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuel_deliveries_pagenumber",
                table: "mobility_fuel_deliveries",
                column: "pagenumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_mobility_lube_deliveries_pagenumber",
                table: "mobility_lube_deliveries");

            migrationBuilder.DropIndex(
                name: "ix_mobility_fuel_deliveries_pagenumber",
                table: "mobility_fuel_deliveries");

            migrationBuilder.DropColumn(
                name: "page_number",
                table: "mobility_lube_purchase_headers");

            migrationBuilder.DropColumn(
                name: "cost",
                table: "mobility_lube_deliveries");

            migrationBuilder.DropColumn(
                name: "freight",
                table: "mobility_lube_deliveries");

            migrationBuilder.DropColumn(
                name: "pagenumber",
                table: "mobility_lube_deliveries");

            migrationBuilder.DropColumn(
                name: "srp",
                table: "mobility_lube_deliveries");

            migrationBuilder.DropColumn(
                name: "page_number",
                table: "mobility_fuel_purchase");

            migrationBuilder.DropColumn(
                name: "pagenumber",
                table: "mobility_fuel_deliveries");

            migrationBuilder.RenameColumn(
                name: "shift_date",
                table: "mobility_lube_purchase_headers",
                newName: "delivery_date");

            migrationBuilder.RenameColumn(
                name: "shiftdate",
                table: "mobility_lube_deliveries",
                newName: "deliverydate");

            migrationBuilder.RenameColumn(
                name: "shift_date",
                table: "mobility_fuel_purchase",
                newName: "delivery_date");

            migrationBuilder.RenameColumn(
                name: "shiftdate",
                table: "mobility_fuel_deliveries",
                newName: "deliverydate");

            migrationBuilder.AddColumn<string>(
                name: "detail_link",
                table: "mobility_lube_purchase_headers",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "shift_rec_id",
                table: "mobility_lube_purchase_headers",
                type: "varchar(20)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "dtllink",
                table: "mobility_lube_deliveries",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "shiftrecid",
                table: "mobility_lube_deliveries",
                type: "varchar(20)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shift_rec_id",
                table: "mobility_fuel_purchase",
                type: "varchar(20)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shiftrecid",
                table: "mobility_fuel_deliveries",
                type: "varchar(20)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_mobility_lube_deliveries_dtllink",
                table: "mobility_lube_deliveries",
                column: "dtllink");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_lube_deliveries_shiftrecid",
                table: "mobility_lube_deliveries",
                column: "shiftrecid");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_fuel_deliveries_shiftrecid",
                table: "mobility_fuel_deliveries",
                column: "shiftrecid");
        }
    }
}
