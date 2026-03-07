using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlfTekPro.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeBankAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employee_bank_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bank_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    account_holder_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    account_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    branch_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    swift_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    iban_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    bank_country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_bank_accounts", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_bank_accounts_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_employee_bank_accounts_employee_id",
                table: "employee_bank_accounts",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_bank_accounts_employee_id_is_primary",
                table: "employee_bank_accounts",
                columns: new[] { "employee_id", "is_primary" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "employee_bank_accounts");
        }
    }
}
