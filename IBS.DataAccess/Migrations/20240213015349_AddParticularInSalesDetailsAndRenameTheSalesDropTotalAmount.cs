using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddParticularInSalesDetailsAndRenameTheSalesDropTotalAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SalesDropTotalAmount",
                table: "SalesHeaders",
                newName: "SafeDropTotalAmount");

            migrationBuilder.AddColumn<string>(
                name: "Particular",
                table: "SalesDetails",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Particular",
                table: "SalesDetails");

            migrationBuilder.RenameColumn(
                name: "SafeDropTotalAmount",
                table: "SalesHeaders",
                newName: "SalesDropTotalAmount");
        }
    }
}
