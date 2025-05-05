using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTheTypeOfSettlementAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "settlement_account_number",
                table: "bienes_placements");

            migrationBuilder.AddColumn<int>(
                name: "settlement_account_id",
                table: "bienes_placements",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_bienes_placements_settlement_account_id",
                table: "bienes_placements",
                column: "settlement_account_id");

            migrationBuilder.AddForeignKey(
                name: "fk_bienes_placements_filpride_bank_accounts_settlement_account",
                table: "bienes_placements",
                column: "settlement_account_id",
                principalTable: "filpride_bank_accounts",
                principalColumn: "bank_account_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_bienes_placements_filpride_bank_accounts_settlement_account",
                table: "bienes_placements");

            migrationBuilder.DropIndex(
                name: "ix_bienes_placements_settlement_account_id",
                table: "bienes_placements");

            migrationBuilder.DropColumn(
                name: "settlement_account_id",
                table: "bienes_placements");

            migrationBuilder.AddColumn<string>(
                name: "settlement_account_number",
                table: "bienes_placements",
                type: "varchar(100)",
                nullable: false,
                defaultValue: "");
        }
    }
}
