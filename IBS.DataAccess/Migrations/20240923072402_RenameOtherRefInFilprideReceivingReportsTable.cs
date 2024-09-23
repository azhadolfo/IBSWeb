using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameOtherRefInFilprideReceivingReportsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "other_ref",
                table: "filpride_receiving_reports",
                newName: "authority_to_load_no");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "authority_to_load_no",
                table: "filpride_receiving_reports",
                newName: "other_ref");
        }
    }
}
