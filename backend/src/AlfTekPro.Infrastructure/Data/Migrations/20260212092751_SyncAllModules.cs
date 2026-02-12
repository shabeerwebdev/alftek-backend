using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlfTekPro.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncAllModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "first_name",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "last_name",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                table: "salary_components",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "salary_components",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AlterColumn<int>(
                name: "radius_meters",
                table: "locations",
                type: "integer",
                nullable: true,
                defaultValue: 100,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 100);

            migrationBuilder.AddColumn<string>(
                name: "city",
                table: "locations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "locations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "contact_email",
                table: "locations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contact_phone",
                table: "locations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "country",
                table: "locations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "locations",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "postal_code",
                table: "locations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "state",
                table: "locations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "leave_types",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "department_id",
                table: "employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "designation_id",
                table: "employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "gender",
                table: "employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "location_id",
                table: "employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "reporting_manager_id",
                table: "employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "designations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "designations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "designations",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "departments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "departments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "head_user_id",
                table: "departments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "departments",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_department_id",
                table: "employees",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_designation_id",
                table: "employees",
                column: "designation_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_location_id",
                table: "employees",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_reporting_manager_id",
                table: "employees",
                column: "reporting_manager_id");

            migrationBuilder.AddForeignKey(
                name: "fk_employees_department_id",
                table: "employees",
                column: "department_id",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_employees_designation_id",
                table: "employees",
                column: "designation_id",
                principalTable: "designations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_employees_location_id",
                table: "employees",
                column: "location_id",
                principalTable: "locations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_employees_reporting_manager_id",
                table: "employees",
                column: "reporting_manager_id",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_employees_department_id",
                table: "employees");

            migrationBuilder.DropForeignKey(
                name: "fk_employees_designation_id",
                table: "employees");

            migrationBuilder.DropForeignKey(
                name: "fk_employees_location_id",
                table: "employees");

            migrationBuilder.DropForeignKey(
                name: "fk_employees_reporting_manager_id",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "IX_employees_department_id",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "IX_employees_designation_id",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "IX_employees_location_id",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "IX_employees_reporting_manager_id",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "first_name",
                table: "users");

            migrationBuilder.DropColumn(
                name: "last_name",
                table: "users");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "salary_components");

            migrationBuilder.DropColumn(
                name: "city",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "code",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "contact_email",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "contact_phone",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "country",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "postal_code",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "state",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "leave_types");

            migrationBuilder.DropColumn(
                name: "department_id",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "designation_id",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "gender",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "location_id",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "reporting_manager_id",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "code",
                table: "designations");

            migrationBuilder.DropColumn(
                name: "description",
                table: "designations");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "designations");

            migrationBuilder.DropColumn(
                name: "code",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "description",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "head_user_id",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "departments");

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                table: "salary_components",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<int>(
                name: "radius_meters",
                table: "locations",
                type: "integer",
                nullable: false,
                defaultValue: 100,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldDefaultValue: 100);
        }
    }
}
