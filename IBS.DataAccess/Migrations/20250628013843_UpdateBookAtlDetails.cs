using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBookAtlDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "load_port",
                table: "filpride_authority_to_loads",
                newName: "depot");

            migrationBuilder.AddColumn<int>(
                name: "appointed_id",
                table: "filpride_book_atl_details",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity",
                table: "filpride_book_atl_details",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "unserved_quantity",
                table: "filpride_book_atl_details",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "load_port_id",
                table: "filpride_authority_to_loads",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_filpride_book_atl_details_appointed_id",
                table: "filpride_book_atl_details",
                column: "appointed_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_book_atl_details_filpride_cos_appointed_suppliers_",
                table: "filpride_book_atl_details",
                column: "appointed_id",
                principalTable: "filpride_cos_appointed_suppliers",
                principalColumn: "sequence_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_book_atl_details_filpride_cos_appointed_suppliers_",
                table: "filpride_book_atl_details");

            migrationBuilder.DropIndex(
                name: "ix_filpride_book_atl_details_appointed_id",
                table: "filpride_book_atl_details");

            migrationBuilder.DropColumn(
                name: "appointed_id",
                table: "filpride_book_atl_details");

            migrationBuilder.DropColumn(
                name: "quantity",
                table: "filpride_book_atl_details");

            migrationBuilder.DropColumn(
                name: "unserved_quantity",
                table: "filpride_book_atl_details");

            migrationBuilder.DropColumn(
                name: "load_port_id",
                table: "filpride_authority_to_loads");

            migrationBuilder.RenameColumn(
                name: "depot",
                table: "filpride_authority_to_loads",
                newName: "load_port");
        }
    }
}
