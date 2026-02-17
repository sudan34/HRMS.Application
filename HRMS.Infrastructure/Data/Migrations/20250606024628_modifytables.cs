using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Application.Migrations
{
    /// <inheritdoc />
    public partial class modifytables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HolidayRecurrence");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "Holidays");

            migrationBuilder.DropColumn(
                name: "IsRecurring",
                table: "Holidays");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Holidays");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedOn",
                table: "Holidays",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

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

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Holidays",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Holidays");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedOn",
                table: "Holidays",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<DateTime>(
                name: "Date",
                table: "Holidays",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsRecurring",
                table: "Holidays",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Holidays",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HolidayRecurrence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HolidayId = table.Column<int>(type: "int", nullable: false),
                    DayOfMonth = table.Column<int>(type: "int", nullable: true),
                    DayOfWeek = table.Column<int>(type: "int", nullable: true),
                    Month = table.Column<int>(type: "int", nullable: true),
                    RecurrenceType = table.Column<int>(type: "int", nullable: false),
                    WeekOfMonth = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HolidayRecurrence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HolidayRecurrence_Holidays_HolidayId",
                        column: x => x.HolidayId,
                        principalTable: "Holidays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HolidayRecurrence_HolidayId",
                table: "HolidayRecurrence",
                column: "HolidayId",
                unique: true);
        }
    }
}
