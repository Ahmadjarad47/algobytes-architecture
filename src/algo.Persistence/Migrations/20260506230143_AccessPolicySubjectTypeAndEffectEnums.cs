using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace algo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AccessPolicySubjectTypeAndEffectEnums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE access_policies
                ALTER COLUMN "Effect" TYPE integer USING (
                    CASE LOWER(BTRIM("Effect"::text))
                        WHEN 'allow' THEN 0
                        WHEN 'deny' THEN 1
                        ELSE 0
                    END
                );
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE access_policies
                ALTER COLUMN "SubjectType" TYPE integer USING (
                    CASE LOWER(BTRIM("SubjectType"::text))
                        WHEN 'user' THEN 0
                        WHEN 'role' THEN 1
                        WHEN 'authenticated' THEN 2
                        WHEN 'everyone' THEN 3
                        ELSE 0
                    END
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE access_policies
                ALTER COLUMN "Effect" TYPE character varying(16) USING (
                    CASE "Effect"
                        WHEN 0 THEN 'allow'
                        WHEN 1 THEN 'deny'
                        ELSE 'allow'
                    END
                );
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE access_policies
                ALTER COLUMN "SubjectType" TYPE character varying(32) USING (
                    CASE "SubjectType"
                        WHEN 0 THEN 'user'
                        WHEN 1 THEN 'role'
                        WHEN 2 THEN 'authenticated'
                        WHEN 3 THEN 'everyone'
                        ELSE 'user'
                    END
                );
                """);
        }
    }
}
