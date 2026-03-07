using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlfTekPro.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveHalfDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add half-day columns to leave_requests
            migrationBuilder.AddColumn<bool>(
                name: "is_half_day",
                table: "leave_requests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "half_day_period",
                table: "leave_requests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            // Add allows_half_day to leave_types
            migrationBuilder.AddColumn<bool>(
                name: "allows_half_day",
                table: "leave_types",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "half_day_period", table: "leave_requests");
            migrationBuilder.DropColumn(name: "is_half_day", table: "leave_requests");
            migrationBuilder.DropColumn(name: "allows_half_day", table: "leave_types");
        }
    }
}
