using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveServiceIdInDmCm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "services_id",
                table: "filpride_debit_memos");

            migrationBuilder.DropColumn(
                name: "services_id",
                table: "filpride_credit_memos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "services_id",
                table: "filpride_debit_memos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "services_id",
                table: "filpride_credit_memos",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
