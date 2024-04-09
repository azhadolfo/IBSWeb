using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPOSalesTotalAmountInSalesHeader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "POSalesTotalAmount",
                table: "SalesHeaders",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "POSalesTotalAmount",
                table: "SalesHeaders");
        }
    }
}
