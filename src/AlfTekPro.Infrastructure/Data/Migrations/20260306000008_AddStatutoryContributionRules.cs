using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlfTekPro.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStatutoryContributionRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "statutory_contribution_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    region_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    party = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    component_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    calculation_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    rate = table.Column<decimal>(type: "decimal(8,4)", nullable: false),
                    max_contribution_base = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    max_contribution_amount = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    effective_from = table.Column<DateTime>(type: "date", nullable: false),
                    effective_to = table.Column<DateTime>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_statutory_contribution_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_statutory_rules_region_id",
                        column: x => x.region_id,
                        principalTable: "regions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_statutory_rules_region_id",
                table: "statutory_contribution_rules",
                column: "region_id");

            migrationBuilder.CreateIndex(
                name: "ix_statutory_rules_region_code",
                table: "statutory_contribution_rules",
                columns: new[] { "region_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "statutory_contribution_rules");
        }
    }
}
