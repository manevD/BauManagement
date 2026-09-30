using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BauManagement.Migrations
{
    /// <inheritdoc />
    public partial class CascadeDeleteWorkTimeEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkTimeEntries_Employees_EmployeeId",
                table: "WorkTimeEntries");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkTimeEntries_Employees_EmployeeId",
                table: "WorkTimeEntries",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkTimeEntries_Employees_EmployeeId",
                table: "WorkTimeEntries");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkTimeEntries_Employees_EmployeeId",
                table: "WorkTimeEntries",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
