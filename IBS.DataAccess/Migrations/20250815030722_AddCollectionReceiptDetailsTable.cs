using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionReceiptDetailsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "filpride_collection_receipt_details",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    collection_receipt_id = table.Column<int>(type: "integer", nullable: false),
                    collection_receipt_no = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    invoice_date = table.Column<DateOnly>(type: "date", nullable: false),
                    invoice_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_collection_receipt_details", x => x.id);
                    table.ForeignKey(
                        name: "fk_filpride_collection_receipt_details_filpride_collection_rec",
                        column: x => x.collection_receipt_id,
                        principalTable: "filpride_collection_receipts",
                        principalColumn: "collection_receipt_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_filpride_collection_receipt_details_collection_receipt_id",
                table: "filpride_collection_receipt_details",
                column: "collection_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_collection_receipt_details_collection_receipt_no",
                table: "filpride_collection_receipt_details",
                column: "collection_receipt_no");

            migrationBuilder.CreateIndex(
                name: "ix_filpride_collection_receipt_details_invoice_no",
                table: "filpride_collection_receipt_details",
                column: "invoice_no");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "filpride_collection_receipt_details");
        }
    }
}
