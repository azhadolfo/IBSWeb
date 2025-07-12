using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddHaulerVatAndTaxTypeInDr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "hauler_name",
                table: "filpride_delivery_receipts",
                type: "varchar(200)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(200)");

            migrationBuilder.AddColumn<string>(
                name: "hauler_tax_type",
                table: "filpride_delivery_receipts",
                type: "varchar(20)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hauler_vat_type",
                table: "filpride_delivery_receipts",
                type: "varchar(20)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "hauler_tax_type",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropColumn(
                name: "hauler_vat_type",
                table: "filpride_delivery_receipts");

            migrationBuilder.AlterColumn<string>(
                name: "hauler_name",
                table: "filpride_delivery_receipts",
                type: "varchar(200)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldNullable: true);
        }
    }
}
