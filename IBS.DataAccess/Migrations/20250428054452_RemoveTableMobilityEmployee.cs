using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTableMobilityEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mobility_check_voucher_headers_mobility_employees_employee_",
                table: "mobility_check_voucher_headers");

            migrationBuilder.DropTable(
                name: "mobility_employees");

            migrationBuilder.CreateIndex(
                name: "ix_mobility_station_employees_employee_number",
                table: "mobility_station_employees",
                column: "employee_number");

            migrationBuilder.AddForeignKey(
                name: "fk_mobility_check_voucher_headers_mobility_station_employees_e",
                table: "mobility_check_voucher_headers",
                column: "employee_id",
                principalTable: "mobility_station_employees",
                principalColumn: "employee_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mobility_check_voucher_headers_mobility_station_employees_e",
                table: "mobility_check_voucher_headers");

            migrationBuilder.DropIndex(
                name: "ix_mobility_station_employees_employee_number",
                table: "mobility_station_employees");

            migrationBuilder.CreateTable(
                name: "mobility_employees",
                columns: table => new
                {
                    employee_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    address = table.Column<string>(type: "varchar(255)", nullable: true),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: true),
                    date_hired = table.Column<DateOnly>(type: "date", nullable: false),
                    date_resigned = table.Column<DateOnly>(type: "date", nullable: true),
                    department = table.Column<string>(type: "text", nullable: true),
                    employee_number = table.Column<string>(type: "varchar(10)", nullable: false),
                    first_name = table.Column<string>(type: "varchar(100)", nullable: false),
                    initial = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_managerial = table.Column<bool>(type: "boolean", nullable: false),
                    last_name = table.Column<string>(type: "varchar(100)", nullable: false),
                    middle_name = table.Column<string>(type: "varchar(100)", nullable: true),
                    pagibig_no = table.Column<string>(type: "text", nullable: true),
                    paygrade = table.Column<string>(type: "text", nullable: true),
                    philhealth_no = table.Column<string>(type: "text", nullable: true),
                    position = table.Column<string>(type: "text", nullable: false),
                    salary = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    sss_no = table.Column<string>(type: "text", nullable: true),
                    station_code = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(5)", nullable: false),
                    suffix = table.Column<string>(type: "varchar(5)", nullable: true),
                    supervisor = table.Column<string>(type: "varchar(20)", nullable: false),
                    tel_no = table.Column<string>(type: "text", nullable: true),
                    tin_no = table.Column<string>(type: "varchar(20)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mobility_employees", x => x.employee_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mobility_employees_employee_number",
                table: "mobility_employees",
                column: "employee_number");

            migrationBuilder.AddForeignKey(
                name: "fk_mobility_check_voucher_headers_mobility_employees_employee_",
                table: "mobility_check_voucher_headers",
                column: "employee_id",
                principalTable: "mobility_employees",
                principalColumn: "employee_id");
        }
    }
}
