using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameCOSUploadUrlsToUploadFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "uploaded_files_url",
                table: "filpride_customer_order_slips",
                newName: "uploaded_files");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "uploaded_files",
                table: "filpride_customer_order_slips",
                newName: "uploaded_files_url");
        }
    }
}
