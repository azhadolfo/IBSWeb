using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CreateHaulersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "haulers",
                columns: table => new
                {
                    hauler_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    hauler_code = table.Column<string>(type: "varchar(3)", nullable: false),
                    hauler_name = table.Column<string>(type: "varchar(100)", nullable: false),
                    hauler_address = table.Column<string>(type: "varchar(200)", nullable: false),
                    contact_no = table.Column<string>(type: "varchar(15)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_haulers", x => x.hauler_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_haulers_hauler_code",
                table: "haulers",
                column: "hauler_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_haulers_hauler_name",
                table: "haulers",
                column: "hauler_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "haulers");
        }
    }
}
