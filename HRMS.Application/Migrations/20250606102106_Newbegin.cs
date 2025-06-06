using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Application.Migrations
{
    /// <inheritdoc />
    public partial class Newbegin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BSDay",
                table: "Holidays");

            migrationBuilder.DropColumn(
                name: "BSMonth",
                table: "Holidays");

            migrationBuilder.DropColumn(
                name: "BSYear",
                table: "Holidays");

            migrationBuilder.AddColumn<DateTime>(
                name: "Date",
                table: "Holidays",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Date",
                table: "Holidays");

            migrationBuilder.AddColumn<int>(
                name: "BSDay",
                table: "Holidays",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BSMonth",
                table: "Holidays",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BSYear",
                table: "Holidays",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
