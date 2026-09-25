using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BauManagement.Migrations
{
    /// <inheritdoc />
    public partial class CompanyErweiterung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Companies",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Companies");
        }
    }
}
