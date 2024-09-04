using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    public partial class RenameTheHaulerToFilprideHauler : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "haulers",
                newName: "filpride_haulers");

            migrationBuilder.RenameIndex(
                name: "ix_haulers_hauler_code",
                table: "filpride_haulers",
                newName: "ix_filpride_haulers_hauler_code");

            migrationBuilder.RenameIndex(
                name: "ix_haulers_hauler_name",
                table: "filpride_haulers",
                newName: "ix_filpride_haulers_hauler_name");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_delivery_receipts_filpride_haulers_hauler_id",
                table: "filpride_delivery_receipts",
                column: "hauler_id",
                principalTable: "filpride_haulers",
                principalColumn: "hauler_id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_delivery_receipts_filpride_haulers_hauler_id",
                table: "filpride_delivery_receipts");

            migrationBuilder.RenameTable(
                name: "filpride_haulers",
                newName: "haulers");

            migrationBuilder.RenameIndex(
                name: "ix_filpride_haulers_hauler_code",
                table: "haulers",
                newName: "ix_haulers_hauler_code");

            migrationBuilder.RenameIndex(
                name: "ix_filpride_haulers_hauler_name",
                table: "haulers",
                newName: "ix_haulers_hauler_name");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_delivery_receipts_haulers_hauler_id",
                table: "filpride_delivery_receipts",
                column: "hauler_id",
                principalTable: "haulers",
                principalColumn: "hauler_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
