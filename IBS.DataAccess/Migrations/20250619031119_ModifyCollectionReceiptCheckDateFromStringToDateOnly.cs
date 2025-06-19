using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ModifyCollectionReceiptCheckDateFromStringToDateOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add a temporary column to store converted dates
            migrationBuilder.AddColumn<DateOnly>(
                name: "check_date_temp",
                table: "filpride_collection_receipts",
                type: "date",
                nullable: true);

            // Step 2: Convert existing text data to date and store in the temp column
            migrationBuilder.Sql("""
                UPDATE filpride_collection_receipts
                SET check_date_temp = CASE
                    WHEN check_date IS NULL THEN NULL
                    WHEN check_date = '' THEN NULL
                    ELSE check_date::date
                END;
            """);

            // Step 3: Drop the old column
            migrationBuilder.DropColumn(
                name: "check_date",
                table: "filpride_collection_receipts");

            // Step 4: Rename the temp column to the original column name
            migrationBuilder.RenameColumn(
                name: "check_date_temp",
                table: "filpride_collection_receipts",
                newName: "check_date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add a temporary column to store text data
            migrationBuilder.AddColumn<string>(
                name: "check_date_temp",
                table: "filpride_collection_receipts",
                type: "text",
                nullable: true);

            // Step 2: Convert date data back to text and store in the temp column
            migrationBuilder.Sql("""
                UPDATE filpride_collection_receipts
                SET check_date_temp = CASE
                    WHEN check_date IS NULL THEN NULL
                    ELSE check_date::text
                END;
            """);

            // Step 3: Drop the old column
            migrationBuilder.DropColumn(
                name: "check_date",
                table: "filpride_collection_receipts");

            // Step 4: Rename the temp column to the original column name
            migrationBuilder.RenameColumn(
                name: "check_date_temp",
                table: "filpride_collection_receipts",
                newName: "check_date");
        }
    }
}
