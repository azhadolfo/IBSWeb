using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameTheIsServedToIsAssignedToDR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "is_served",
                table: "filpride_cos_appointed_suppliers",
                newName: "is_assigned_to_dr");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "is_assigned_to_dr",
                table: "filpride_cos_appointed_suppliers",
                newName: "is_served");
        }
    }
}
