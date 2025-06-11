using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddBienesPlacementsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bienes_placements",
                columns: table => new
                {
                    placement_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    control_number = table.Column<string>(type: "varchar(20)", nullable: false),
                    company_id = table.Column<int>(type: "integer", nullable: false),
                    bank_id = table.Column<int>(type: "integer", nullable: false),
                    bank = table.Column<string>(type: "varchar(20)", nullable: false),
                    branch = table.Column<string>(type: "varchar(100)", nullable: false),
                    account_name = table.Column<string>(type: "varchar(100)", nullable: false),
                    @class = table.Column<string>(name: "class", type: "varchar(10)", nullable: false),
                    settlement_account_number = table.Column<string>(type: "varchar(100)", nullable: false),
                    date_from = table.Column<DateOnly>(type: "date", nullable: false),
                    date_to = table.Column<DateOnly>(type: "date", nullable: false),
                    remarks = table.Column<string>(type: "varchar(255)", nullable: false),
                    cheque_number = table.Column<string>(type: "varchar(100)", nullable: false),
                    cv_no = table.Column<string>(type: "varchar(100)", nullable: false),
                    disposition = table.Column<string>(type: "varchar(5)", nullable: false),
                    principal_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    principal_disposition = table.Column<string>(type: "varchar(100)", nullable: false),
                    placement_type = table.Column<int>(type: "integer", nullable: false),
                    number_of_years = table.Column<int>(type: "integer", nullable: false),
                    interest_rate = table.Column<decimal>(type: "numeric(3,10)", nullable: false),
                    has_ewt = table.Column<bool>(type: "boolean", nullable: false),
                    ewt_rate = table.Column<decimal>(type: "numeric(3,4)", nullable: false),
                    has_trust_fee = table.Column<bool>(type: "boolean", nullable: false),
                    trust_fee_rate = table.Column<decimal>(type: "numeric(3,8)", nullable: false),
                    interest_deposited = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    interest_deposited_to = table.Column<string>(type: "text", nullable: true),
                    interest_deposited_date = table.Column<DateOnly>(type: "date", nullable: true),
                    frequency_of_payment = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    posted_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    terminated_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    terminated_date = table.Column<DateOnly>(type: "date", nullable: true),
                    termination_remarks = table.Column<string>(type: "varchar(255)", nullable: true),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false),
                    locked_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    interest_status = table.Column<string>(type: "varchar(50)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bienes_placements", x => x.placement_id);
                    table.ForeignKey(
                        name: "fk_bienes_placements_bienes_bank_accounts_bank_id",
                        column: x => x.bank_id,
                        principalTable: "bienes_bank_accounts",
                        principalColumn: "bank_account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bienes_placements_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "company_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bienes_placements_bank_id",
                table: "bienes_placements",
                column: "bank_id");

            migrationBuilder.CreateIndex(
                name: "ix_bienes_placements_company_id",
                table: "bienes_placements",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_bienes_placements_control_number",
                table: "bienes_placements",
                column: "control_number");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bienes_placements");
        }
    }
}
