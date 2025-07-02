using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameTheAtlIdInFilprideDr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_delivery_receipts_filpride_authority_to_loads_atl_",
                table: "filpride_delivery_receipts");

            migrationBuilder.RenameColumn(
                name: "atl_id",
                table: "filpride_delivery_receipts",
                newName: "authority_to_load_id");

            migrationBuilder.RenameIndex(
                name: "ix_filpride_delivery_receipts_atl_id",
                table: "filpride_delivery_receipts",
                newName: "ix_filpride_delivery_receipts_authority_to_load_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_delivery_receipts_filpride_authority_to_loads_auth",
                table: "filpride_delivery_receipts",
                column: "authority_to_load_id",
                principalTable: "filpride_authority_to_loads",
                principalColumn: "authority_to_load_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_delivery_receipts_filpride_authority_to_loads_auth",
                table: "filpride_delivery_receipts");

            migrationBuilder.RenameColumn(
                name: "authority_to_load_id",
                table: "filpride_delivery_receipts",
                newName: "atl_id");

            migrationBuilder.RenameIndex(
                name: "ix_filpride_delivery_receipts_authority_to_load_id",
                table: "filpride_delivery_receipts",
                newName: "ix_filpride_delivery_receipts_atl_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_delivery_receipts_filpride_authority_to_loads_atl_",
                table: "filpride_delivery_receipts",
                column: "atl_id",
                principalTable: "filpride_authority_to_loads",
                principalColumn: "authority_to_load_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
