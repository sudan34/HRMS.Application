using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Application.Migrations
{
    /// <inheritdoc />
    public partial class add_unkownattendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UnknownAttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrollmentNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecordTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InOutMode = table.Column<int>(type: "int", nullable: false),
                    Processed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnknownAttendanceRecords", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnknownAttendanceRecords");
        }
    }
}
