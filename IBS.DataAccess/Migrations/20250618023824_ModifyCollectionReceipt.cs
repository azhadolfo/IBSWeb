using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ModifyCollectionReceipt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "check_bank",
                table: "filpride_collection_receipts");

            migrationBuilder.DropColumn(
                name: "series_number",
                table: "filpride_collection_receipts");

            migrationBuilder.AddColumn<int>(
                name: "bank_id",
                table: "filpride_collection_receipts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_collection_receipts_bank_id",
                table: "filpride_collection_receipts",
                column: "bank_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_collection_receipts_filpride_bank_accounts_bank_id",
                table: "filpride_collection_receipts",
                column: "bank_id",
                principalTable: "filpride_bank_accounts",
                principalColumn: "bank_account_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_collection_receipts_filpride_bank_accounts_bank_id",
                table: "filpride_collection_receipts");

            migrationBuilder.DropIndex(
                name: "ix_filpride_collection_receipts_bank_id",
                table: "filpride_collection_receipts");

            migrationBuilder.DropColumn(
                name: "bank_id",
                table: "filpride_collection_receipts");

            migrationBuilder.AddColumn<string>(
                name: "check_bank",
                table: "filpride_collection_receipts",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "series_number",
                table: "filpride_collection_receipts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
