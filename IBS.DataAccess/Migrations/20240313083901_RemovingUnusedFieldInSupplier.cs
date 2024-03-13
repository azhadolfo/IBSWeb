using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemovingUnusedFieldInSupplier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProofOfExemptionFilePath",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "ReasonOfExemption",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "Validity",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "ValidityDate",
                table: "Suppliers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProofOfExemptionFilePath",
                table: "Suppliers",
                type: "varchar(200)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReasonOfExemption",
                table: "Suppliers",
                type: "varchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Validity",
                table: "Suppliers",
                type: "varchar(20)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidityDate",
                table: "Suppliers",
                type: "date",
                nullable: true);
        }
    }
}
