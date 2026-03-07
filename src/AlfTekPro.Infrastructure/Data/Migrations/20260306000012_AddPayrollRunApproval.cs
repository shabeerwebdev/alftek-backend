using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlfTekPro.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollRunApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "approved_by",
                table: "payroll_runs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "approved_at",
                table: "payroll_runs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                table: "payroll_runs",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "approved_by", table: "payroll_runs");
            migrationBuilder.DropColumn(name: "approved_at", table: "payroll_runs");
            migrationBuilder.DropColumn(name: "rejection_reason", table: "payroll_runs");
        }
    }
}
