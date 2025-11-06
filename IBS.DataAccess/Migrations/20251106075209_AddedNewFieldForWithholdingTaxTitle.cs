using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddedNewFieldForWithholdingTaxTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "withholding_taxtitle",
                table: "filpride_suppliers",
                newName: "withholding_tax_title");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "withholding_tax_title",
                table: "filpride_suppliers",
                newName: "withholding_taxtitle");
        }
    }
}
