using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CreateAuthorityToLoadTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "authority_to_load_id",
                table: "filpride_customer_order_slips",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "filpride_authority_to_loads",
                columns: table => new
                {
                    authority_to_load_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    authority_to_load_no = table.Column<string>(type: "varchar(20)", nullable: false),
                    date_booked = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_until = table.Column<DateOnly>(type: "date", nullable: false),
                    uppi_atl_no = table.Column<string>(type: "varchar(20)", nullable: true),
                    remarks = table.Column<string>(type: "varchar(255)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(20)", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_authority_to_loads", x => x.authority_to_load_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_filpride_customer_order_slips_authority_to_load_id",
                table: "filpride_customer_order_slips",
                column: "authority_to_load_id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_authority_to_loads_a",
                table: "filpride_customer_order_slips",
                column: "authority_to_load_id",
                principalTable: "filpride_authority_to_loads",
                principalColumn: "authority_to_load_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_customer_order_slips_filpride_authority_to_loads_a",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropTable(
                name: "filpride_authority_to_loads");

            migrationBuilder.DropIndex(
                name: "ix_filpride_customer_order_slips_authority_to_load_id",
                table: "filpride_customer_order_slips");

            migrationBuilder.DropColumn(
                name: "authority_to_load_id",
                table: "filpride_customer_order_slips");
        }
    }
}
