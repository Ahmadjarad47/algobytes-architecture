using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace algo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductDualCurrencyPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountedPriceSyp",
                table: "Products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountedPriceUsd",
                table: "Products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceSyp",
                table: "Products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceUsd",
                table: "Products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Products"
                SET "PriceUsd" = "Price",
                    "DiscountedPriceUsd" = "DiscountedPrice"
                WHERE UPPER("CurrencyCode") = 'USD';
                """);

            migrationBuilder.Sql("""
                UPDATE "Products"
                SET "PriceSyp" = "Price",
                    "DiscountedPriceSyp" = "DiscountedPrice"
                WHERE UPPER("CurrencyCode") = 'SYP';
                """);

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DiscountedPrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "Products",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "USD");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountedPrice",
                table: "Products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""
                UPDATE "Products"
                SET "CurrencyCode" = 'USD',
                    "Price" = COALESCE("PriceUsd", 0),
                    "DiscountedPrice" = "DiscountedPriceUsd"
                WHERE "PriceUsd" IS NOT NULL OR "DiscountedPriceUsd" IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE "Products"
                SET "CurrencyCode" = 'SYP',
                    "Price" = COALESCE("PriceSyp", 0),
                    "DiscountedPrice" = "DiscountedPriceSyp"
                WHERE "PriceUsd" IS NULL
                  AND "DiscountedPriceUsd" IS NULL
                  AND ("PriceSyp" IS NOT NULL OR "DiscountedPriceSyp" IS NOT NULL);
                """);

            migrationBuilder.DropColumn(
                name: "DiscountedPriceSyp",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DiscountedPriceUsd",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PriceSyp",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PriceUsd",
                table: "Products");
        }
    }
}
