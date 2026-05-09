using System;
using algo.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace algo.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260509113000_ActiveSessionsMetadata")]
public partial class ActiveSessionsMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Browser",
            table: "RefreshTokens",
            type: "character varying(80)",
            maxLength: 80,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Device",
            table: "RefreshTokens",
            type: "character varying(80)",
            maxLength: 80,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "IpAddress",
            table: "RefreshTokens",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsSuspicious",
            table: "RefreshTokens",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "IsTrustedDevice",
            table: "RefreshTokens",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "LastActivityAt",
            table: "RefreshTokens",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "NOW()");

        migrationBuilder.AddColumn<string>(
            name: "Location",
            table: "RefreshTokens",
            type: "character varying(160)",
            maxLength: 160,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "OperatingSystem",
            table: "RefreshTokens",
            type: "character varying(80)",
            maxLength: 80,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "RevokedByUserId",
            table: "RefreshTokens",
            type: "character varying(450)",
            maxLength: 450,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "UserAgent",
            table: "RefreshTokens",
            type: "character varying(512)",
            maxLength: 512,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_ExpiresAt",
            table: "RefreshTokens",
            column: "ExpiresAt");

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_RevokedAt",
            table: "RefreshTokens",
            column: "RevokedAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_RefreshTokens_ExpiresAt",
            table: "RefreshTokens");

        migrationBuilder.DropIndex(
            name: "IX_RefreshTokens_RevokedAt",
            table: "RefreshTokens");

        migrationBuilder.DropColumn(name: "Browser", table: "RefreshTokens");
        migrationBuilder.DropColumn(name: "Device", table: "RefreshTokens");
        migrationBuilder.DropColumn(name: "IpAddress", table: "RefreshTokens");
        migrationBuilder.DropColumn(name: "IsSuspicious", table: "RefreshTokens");
        migrationBuilder.DropColumn(name: "IsTrustedDevice", table: "RefreshTokens");
        migrationBuilder.DropColumn(name: "LastActivityAt", table: "RefreshTokens");
        migrationBuilder.DropColumn(name: "Location", table: "RefreshTokens");
        migrationBuilder.DropColumn(name: "OperatingSystem", table: "RefreshTokens");
        migrationBuilder.DropColumn(name: "RevokedByUserId", table: "RefreshTokens");
        migrationBuilder.DropColumn(name: "UserAgent", table: "RefreshTokens");
    }
}
