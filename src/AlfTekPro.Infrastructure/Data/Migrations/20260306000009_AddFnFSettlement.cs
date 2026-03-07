using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlfTekPro.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFnFSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fnf_settlements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_working_day = table.Column<DateTime>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    unpaid_salary = table.Column<decimal>(type: "decimal(14,2)", nullable: false, defaultValue: 0m),
                    gratuity_amount = table.Column<decimal>(type: "decimal(14,2)", nullable: false, defaultValue: 0m),
                    unused_leave_encashment = table.Column<decimal>(type: "decimal(14,2)", nullable: false, defaultValue: 0m),
                    other_earnings = table.Column<decimal>(type: "decimal(14,2)", nullable: false, defaultValue: 0m),
                    loan_deductions = table.Column<decimal>(type: "decimal(14,2)", nullable: false, defaultValue: 0m),
                    tax_deductions = table.Column<decimal>(type: "decimal(14,2)", nullable: false, defaultValue: 0m),
                    other_deductions = table.Column<decimal>(type: "decimal(14,2)", nullable: false, defaultValue: 0m),
                    total_earnings = table.Column<decimal>(type: "decimal(14,2)", nullable: false, defaultValue: 0m),
                    total_deductions = table.Column<decimal>(type: "decimal(14,2)", nullable: false, defaultValue: 0m),
                    net_settlement_amount = table.Column<decimal>(type: "decimal(14,2)", nullable: false, defaultValue: 0m),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fnf_settlements", x => x.id);
                    table.ForeignKey(
                        name: "fk_fnf_settlements_approved_by",
                        column: x => x.approved_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_fnf_settlements_tenant_id",
                table: "fnf_settlements",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_fnf_settlements_employee_id",
                table: "fnf_settlements",
                column: "employee_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "fnf_settlements");
        }
    }
}
