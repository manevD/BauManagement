using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BauManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyScopeToAssignmentsAndTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "WorkTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "WorkAssignments",
                type: "uniqueidentifier",
                nullable: true);

            // Existing records already belong to a tenant through their required
            // Mitarbeiter or Baustelle relationship. Backfill before enforcing it.
            migrationBuilder.Sql("""
                UPDATE wa
                SET CompanyId = e.CompanyId
                FROM WorkAssignments wa
                INNER JOIN Employees e ON e.Id = wa.EmployeeId;
                """);

            migrationBuilder.Sql("""
                UPDATE wt
                SET CompanyId = b.CompanyId
                FROM WorkTasks wt
                INNER JOIN Baustellen b ON b.Id = wt.BaustelleId;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "WorkTasks",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "WorkAssignments",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_CompanyId",
                table: "WorkTasks",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignments_CompanyId",
                table: "WorkAssignments",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkAssignments_Companies_CompanyId",
                table: "WorkAssignments",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkTasks_Companies_CompanyId",
                table: "WorkTasks",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkAssignments_Companies_CompanyId",
                table: "WorkAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkTasks_Companies_CompanyId",
                table: "WorkTasks");

            migrationBuilder.DropIndex(
                name: "IX_WorkTasks_CompanyId",
                table: "WorkTasks");

            migrationBuilder.DropIndex(
                name: "IX_WorkAssignments_CompanyId",
                table: "WorkAssignments");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "WorkTasks");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "WorkAssignments");
        }
    }
}
