using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Application.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEmployeeIdAsForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceSummaries_Employees_EmployeeId1",
                table: "AttendanceSummaries");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceSummaries_EmployeeId1",
                table: "AttendanceSummaries");

            migrationBuilder.DropColumn(
                name: "EmployeeId1",
                table: "AttendanceSummaries");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSummaries_EmployeeId",
                table: "AttendanceSummaries",
                column: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceSummaries_Employees_EmployeeId",
                table: "AttendanceSummaries",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceSummaries_Employees_EmployeeId",
                table: "AttendanceSummaries");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceSummaries_EmployeeId",
                table: "AttendanceSummaries");

            migrationBuilder.AddColumn<int>(
                name: "EmployeeId1",
                table: "AttendanceSummaries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSummaries_EmployeeId1",
                table: "AttendanceSummaries",
                column: "EmployeeId1");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceSummaries_Employees_EmployeeId1",
                table: "AttendanceSummaries",
                column: "EmployeeId1",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
