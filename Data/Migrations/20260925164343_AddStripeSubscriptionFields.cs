using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BauManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeSubscriptionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CurrentPeriodEnd",
                table: "Companies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripePriceId",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentPeriodEnd",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "StripePriceId",
                table: "Companies");
        }
    }
}
