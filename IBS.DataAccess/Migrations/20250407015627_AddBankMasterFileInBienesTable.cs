using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddBankMasterFileInBienesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bienes_bank_accounts",
                columns: table => new
                {
                    bank_account_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bank = table.Column<string>(type: "text", nullable: false),
                    branch = table.Column<string>(type: "text", nullable: false),
                    account_no = table.Column<string>(type: "text", nullable: false),
                    account_name = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    company = table.Column<string>(type: "text", nullable: false)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bienes_bank_accounts");
        }
    }
}
