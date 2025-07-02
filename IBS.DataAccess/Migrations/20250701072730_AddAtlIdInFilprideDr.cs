using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAtlIdInFilprideDr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "hauler_name",
                table: "filpride_delivery_receipts",
                type: "varchar(200)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "atl_id",
                table: "filpride_delivery_receipts",
                type: "integer",
                nullable: false,
                defaultValue: 1);//Remove the value after migrating

            migrationBuilder.CreateIndex(
                name: "ix_filpride_delivery_receipts_atl_id",
                table: "filpride_delivery_receipts",
                column: "atl_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_delivery_receipts_filpride_authority_to_loads_atl_",
                table: "filpride_delivery_receipts",
                column: "atl_id",
                principalTable: "filpride_authority_to_loads",
                principalColumn: "authority_to_load_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_delivery_receipts_filpride_authority_to_loads_atl_",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropIndex(
                name: "ix_filpride_delivery_receipts_atl_id",
                table: "filpride_delivery_receipts");

            migrationBuilder.DropColumn(
                name: "atl_id",
                table: "filpride_delivery_receipts");

            migrationBuilder.AlterColumn<string>(
                name: "hauler_name",
                table: "filpride_delivery_receipts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)");
        }
    }
}
