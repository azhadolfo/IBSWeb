using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddNetOfVatAmountToFilprideInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "net_of_vat_amount",
                table: "filpride_inventories",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "vat_type",
                table: "filpride_inventories",
                type: "character varying(20)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "net_of_vat_amount",
                table: "filpride_inventories");

            migrationBuilder.DropColumn(
                name: "vat_type",
                table: "filpride_inventories");
        }
    }
}
