using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace algo.Persistence.Migrations
{
    public partial class DynamicCustomFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<JsonDocument>(
                name: "CustomFields",
                table: "AspNetUsers",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<JsonDocument>(
                name: "CustomFields",
                table: "AspNetRoles",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<JsonDocument>(
                name: "CustomFields",
                table: "access_policies",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "custom_field_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Entity = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Label = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    Searchable = table.Column<bool>(type: "boolean", nullable: false),
                    Filterable = table.Column<bool>(type: "boolean", nullable: false),
                    Sortable = table.Column<bool>(type: "boolean", nullable: false),
                    VisibleInTable = table.Column<bool>(type: "boolean", nullable: false),
                    VisibleInForm = table.Column<bool>(type: "boolean", nullable: false),
                    VisibleInDetails = table.Column<bool>(type: "boolean", nullable: false),
                    OptionsJson = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    DefaultValueJson = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    ValidationJson = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_custom_field_definitions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_custom_field_definitions_Entity_Key",
                table: "custom_field_definitions",
                columns: new[] { "Entity", "Key" },
                unique: true);

            if (ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql("""
                    CREATE INDEX IF NOT EXISTS "IX_AspNetUsers_CustomFields"
                    ON "AspNetUsers" USING gin ("CustomFields");
                    """);
                migrationBuilder.Sql("""
                    CREATE INDEX IF NOT EXISTS "IX_AspNetRoles_CustomFields"
                    ON "AspNetRoles" USING gin ("CustomFields");
                    """);
                migrationBuilder.Sql("""
                    CREATE INDEX IF NOT EXISTS "IX_access_policies_CustomFields"
                    ON "access_policies" USING gin ("CustomFields");
                    """);
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_AspNetUsers_CustomFields";""");
                migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_AspNetRoles_CustomFields";""");
                migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_access_policies_CustomFields";""");
            }

            migrationBuilder.DropTable(
                name: "custom_field_definitions");

            migrationBuilder.DropColumn(
                name: "CustomFields",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CustomFields",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "CustomFields",
                table: "access_policies");
        }
    }
}
