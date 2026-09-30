using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BauManagement.Migrations
{
    /// <inheritdoc />
    public partial class FixDeleteRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeVacations_Employees_EmployeeId",
                table: "EmployeeVacations");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkAssignments_Baustellen_BaustelleId",
                table: "WorkAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkAssignments_Employees_EmployeeId",
                table: "WorkAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkTasks_Baustellen_BaustelleId",
                table: "WorkTasks");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeVacations_Employees_EmployeeId",
                table: "EmployeeVacations",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkAssignments_Baustellen_BaustelleId",
                table: "WorkAssignments",
                column: "BaustelleId",
                principalTable: "Baustellen",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkAssignments_Employees_EmployeeId",
                table: "WorkAssignments",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkTasks_Baustellen_BaustelleId",
                table: "WorkTasks",
                column: "BaustelleId",
                principalTable: "Baustellen",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeVacations_Employees_EmployeeId",
                table: "EmployeeVacations");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkAssignments_Baustellen_BaustelleId",
                table: "WorkAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkAssignments_Employees_EmployeeId",
                table: "WorkAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkTasks_Baustellen_BaustelleId",
                table: "WorkTasks");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeVacations_Employees_EmployeeId",
                table: "EmployeeVacations",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkAssignments_Baustellen_BaustelleId",
                table: "WorkAssignments",
                column: "BaustelleId",
                principalTable: "Baustellen",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkAssignments_Employees_EmployeeId",
                table: "WorkAssignments",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkTasks_Baustellen_BaustelleId",
                table: "WorkTasks",
                column: "BaustelleId",
                principalTable: "Baustellen",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
