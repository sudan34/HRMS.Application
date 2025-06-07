using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Application.Migrations
{
    /// <inheritdoc />
    public partial class Update_workignhour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiredWorkHours",
                table: "DepartmentWorkingHours");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "FridayEndTime",
                table: "DepartmentWorkingHours",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "FridayLateThreshold",
                table: "DepartmentWorkingHours",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "FridayStartTime",
                table: "DepartmentWorkingHours",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<bool>(
                name: "IsFriday",
                table: "DepartmentWorkingHours",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FridayEndTime",
                table: "DepartmentWorkingHours");

            migrationBuilder.DropColumn(
                name: "FridayLateThreshold",
                table: "DepartmentWorkingHours");

            migrationBuilder.DropColumn(
                name: "FridayStartTime",
                table: "DepartmentWorkingHours");

            migrationBuilder.DropColumn(
                name: "IsFriday",
                table: "DepartmentWorkingHours");

            migrationBuilder.AddColumn<double>(
                name: "RequiredWorkHours",
                table: "DepartmentWorkingHours",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
