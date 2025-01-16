using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CreateBookAtlDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "filpride_book_atl_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    authority_to_load_id = table.Column<int>(type: "integer", nullable: false),
                    customer_order_slip_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_book_atl_details", x => x.id);
                    table.ForeignKey(
                        name: "fk_filpride_book_atl_details_filpride_authority_to_loads_autho",
                        column: x => x.authority_to_load_id,
                        principalTable: "filpride_authority_to_loads",
                        principalColumn: "authority_to_load_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_filpride_book_atl_details_filpride_customer_order_slips_cus",
                        column: x => x.customer_order_slip_id,
                        principalTable: "filpride_customer_order_slips",
                        principalColumn: "customer_order_slip_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_filpride_book_atl_details_authority_to_load_id",
                table: "filpride_book_atl_details",
                column: "authority_to_load_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_book_atl_details_customer_order_slip_id",
                table: "filpride_book_atl_details",
                column: "customer_order_slip_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "filpride_book_atl_details");
        }
    }
}
