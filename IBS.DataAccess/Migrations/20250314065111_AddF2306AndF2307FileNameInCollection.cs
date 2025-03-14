using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddF2306AndF2307FileNameInCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "f2306file_name",
                table: "filpride_collection_receipts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "f2307file_name",
                table: "filpride_collection_receipts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "f2306file_name",
                table: "filpride_collection_receipts");

            migrationBuilder.DropColumn(
                name: "f2307file_name",
                table: "filpride_collection_receipts");
        }
    }
}
