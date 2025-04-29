using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDocumentedMMSICollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mmsi_collections",
                columns: table => new
                {
                    mmsi_collection_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    collection_number = table.Column<string>(type: "text", nullable: false),
                    check_number = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    check_date = table.Column<DateOnly>(type: "date", nullable: false),
                    deposit_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    ewt = table.Column<decimal>(type: "numeric", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: true),
                    is_documented = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_collections", x => x.mmsi_collection_id);
                    table.ForeignKey(
                        name: "fk_mmsi_collections_mmsi_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "mmsi_customers",
                        principalColumn: "mmsi_customer_id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_collections_customer_id",
                table: "mmsi_collections",
                column: "customer_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mmsi_collections");
        }
    }
}
