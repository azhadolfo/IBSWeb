using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ChangeFromUidToIntegerThePickUpPointTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_filpride_pick_up_points",
                table: "filpride_pick_up_points");

            migrationBuilder.DropColumn(
                name: "id",
                table: "filpride_pick_up_points");

            migrationBuilder.AddColumn<int>(
                name: "pick_up_point_id",
                table: "filpride_pick_up_points",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "pk_filpride_pick_up_points",
                table: "filpride_pick_up_points",
                column: "pick_up_point_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_filpride_pick_up_points",
                table: "filpride_pick_up_points");

            migrationBuilder.DropColumn(
                name: "pick_up_point_id",
                table: "filpride_pick_up_points");

            migrationBuilder.AddColumn<Guid>(
                name: "id",
                table: "filpride_pick_up_points",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "pk_filpride_pick_up_points",
                table: "filpride_pick_up_points",
                column: "id");
        }
    }
}
