using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomerPortal.UserAuthService.Postgres.Migrations;

/// <inheritdoc />
public partial class AddCollation : Migration
{
    public const string CaseInsensitiveCollationName = "customerportalemailci";

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            $@"
                CREATE COLLATION IF NOT EXISTS {CaseInsensitiveCollationName} (
                    provider = icu,
                    locale = 'und-u-ks-level2',
                    deterministic = false
                );
            "
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"DROP COLLATION IF EXISTS {CaseInsensitiveCollationName};");
    }
}
