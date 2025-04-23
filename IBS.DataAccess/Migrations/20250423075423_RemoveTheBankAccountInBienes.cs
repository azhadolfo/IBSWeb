using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTheBankAccountInBienes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_bienes_placements_bienes_bank_accounts_bank_id",
                table: "bienes_placements");

            migrationBuilder.DropTable(
                name: "bienes_bank_accounts");

            migrationBuilder.AddForeignKey(
                name: "fk_bienes_placements_filpride_bank_accounts_bank_id",
                table: "bienes_placements",
                column: "bank_id",
                principalTable: "filpride_bank_accounts",
                principalColumn: "bank_account_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_bienes_placements_filpride_bank_accounts_bank_id",
                table: "bienes_placements");

            migrationBuilder.CreateTable(
                name: "bienes_bank_accounts",
                columns: table => new
                {
                    bank_account_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_name = table.Column<string>(type: "text", nullable: false),
                    account_no = table.Column<string>(type: "text", nullable: false),
                    bank = table.Column<string>(type: "text", nullable: false),
                    branch = table.Column<string>(type: "text", nullable: false),
                    company = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bienes_bank_accounts", x => x.bank_account_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bienes_bank_accounts_account_no",
                table: "bienes_bank_accounts",
                column: "account_no");

            migrationBuilder.CreateIndex(
                name: "ix_bienes_bank_accounts_bank",
                table: "bienes_bank_accounts",
                column: "bank");

            migrationBuilder.AddForeignKey(
                name: "fk_bienes_placements_bienes_bank_accounts_bank_id",
                table: "bienes_placements",
                column: "bank_id",
                principalTable: "bienes_bank_accounts",
                principalColumn: "bank_account_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
