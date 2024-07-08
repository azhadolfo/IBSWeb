using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLoadPortFieldInFilprideDeliveryReceiptsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "load_port",
                table: "filpride_delivery_receipts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "load_port",
                table: "filpride_delivery_receipts",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "");
        }
    }
}
