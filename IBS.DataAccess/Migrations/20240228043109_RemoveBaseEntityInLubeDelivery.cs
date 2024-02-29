using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBaseEntityInLubeDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanceledBy",
                table: "LubeDeliveries");

            migrationBuilder.DropColumn(
                name: "CanceledDate",
                table: "LubeDeliveries");

            migrationBuilder.DropColumn(
                name: "EditedBy",
                table: "LubeDeliveries");

            migrationBuilder.DropColumn(
                name: "EditedDate",
                table: "LubeDeliveries");

            migrationBuilder.DropColumn(
                name: "PostedBy",
                table: "LubeDeliveries");

            migrationBuilder.DropColumn(
                name: "PostedDate",
                table: "LubeDeliveries");

            migrationBuilder.DropColumn(
                name: "VoidedBy",
                table: "LubeDeliveries");

            migrationBuilder.DropColumn(
                name: "VoidedDate",
                table: "LubeDeliveries");

            migrationBuilder.DropColumn(
                name: "receivedby",
                table: "LubeDeliveries");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "LubeDeliveries",
                newName: "createddate");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "LubeDeliveries",
                newName: "createdby");

            migrationBuilder.AlterColumn<DateTime>(
                name: "createddate",
                table: "LubeDeliveries",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "createdby",
                table: "LubeDeliveries",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rcvdby",
                table: "LubeDeliveries",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "rcvdby",
                table: "LubeDeliveries");

            migrationBuilder.RenameColumn(
                name: "createddate",
                table: "LubeDeliveries",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "createdby",
                table: "LubeDeliveries",
                newName: "CreatedBy");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "LubeDeliveries",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "LubeDeliveries",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AddColumn<string>(
                name: "CanceledBy",
                table: "LubeDeliveries",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CanceledDate",
                table: "LubeDeliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EditedBy",
                table: "LubeDeliveries",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EditedDate",
                table: "LubeDeliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostedBy",
                table: "LubeDeliveries",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PostedDate",
                table: "LubeDeliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidedBy",
                table: "LubeDeliveries",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedDate",
                table: "LubeDeliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "receivedby",
                table: "LubeDeliveries",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
