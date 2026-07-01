using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace algo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductImageAndStorageConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StorageConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EndpointUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    AccessKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SecretKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    BucketName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Region = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Folder = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    UsePathStyle = table.Column<bool>(type: "boolean", nullable: false),
                    ScannerEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ScannerProvider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ScannerEndpointUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ScannerApiKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    QuarantineFolder = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageConfigurations", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StorageConfigurations");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Products");
        }
    }
}
