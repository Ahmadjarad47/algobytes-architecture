using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace algo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UserTotpPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TotpRequiredByAdmin",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotpRequiredByAdmin",
                table: "AspNetUsers");
        }
    }
}
