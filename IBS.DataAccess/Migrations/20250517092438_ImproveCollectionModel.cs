using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ImproveCollectionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_collections_filpride_customers_customer_id",
                table: "mmsi_collections");

            migrationBuilder.AlterColumn<string>(
                name: "mmsi_collection_number",
                table: "mmsi_collections",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "customer_id",
                table: "mmsi_collections",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_date",
                table: "mmsi_collections",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "mmsi_collections",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "mmsi_collection_id",
                table: "mmsi_billings",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_billings_mmsi_collection_id",
                table: "mmsi_billings",
                column: "mmsi_collection_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_billings_mmsi_collections_mmsi_collection_id",
                table: "mmsi_billings",
                column: "mmsi_collection_id",
                principalTable: "mmsi_collections",
                principalColumn: "mmsi_collection_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_collections_filpride_customers_customer_id",
                table: "mmsi_collections",
                column: "customer_id",
                principalTable: "filpride_customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_billings_mmsi_collections_mmsi_collection_id",
                table: "mmsi_billings");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_collections_filpride_customers_customer_id",
                table: "mmsi_collections");

            migrationBuilder.DropIndex(
                name: "ix_mmsi_billings_mmsi_collection_id",
                table: "mmsi_billings");

            migrationBuilder.DropColumn(
                name: "mmsi_collection_id",
                table: "mmsi_billings");

            migrationBuilder.AlterColumn<string>(
                name: "mmsi_collection_number",
                table: "mmsi_collections",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "customer_id",
                table: "mmsi_collections",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_date",
                table: "mmsi_collections",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "mmsi_collections",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_collections_filpride_customers_customer_id",
                table: "mmsi_collections",
                column: "customer_id",
                principalTable: "filpride_customers",
                principalColumn: "customer_id");
        }
    }
}
