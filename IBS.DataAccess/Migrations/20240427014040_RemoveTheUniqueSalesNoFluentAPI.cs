using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTheUniqueSalesNoFluentAPI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesHeaders_SalesNo",
                table: "SalesHeaders");

            migrationBuilder.CreateIndex(
                name: "IX_SalesHeaders_SalesNo",
                table: "SalesHeaders",
                column: "SalesNo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesHeaders_SalesNo",
                table: "SalesHeaders");

            migrationBuilder.CreateIndex(
                name: "IX_SalesHeaders_SalesNo",
                table: "SalesHeaders",
                column: "SalesNo",
                unique: true);
        }
    }
}
