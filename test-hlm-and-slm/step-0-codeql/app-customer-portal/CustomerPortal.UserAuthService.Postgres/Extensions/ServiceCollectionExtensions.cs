using CustomerPortal.UserAuthService.Domain.Repositories;
using CustomerPortal.UserAuthService.Postgres.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CustomerPortal.UserAuthService.Postgres.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddUserAuthServicePostgres(
        this IServiceCollection services,
        Action<PostgresOptions> configureOptions
    )
    {
        services.Configure(configureOptions);

        services.AddDbContextPool<UserAuthContext>(
            (serviceProvider, o) =>
                o.UseNpgsql(
                    serviceProvider
                        .GetRequiredService<IOptions<PostgresOptions>>()
                        .Value.ConnectionString
                )
        );

        services.AddTransient<IUserRepository, DbContextUserRepository>();
    }

    public static async Task InitializeUserAuthServicePostgres(
        this IServiceProvider serviceProvider
    )
    {
        var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<UserAuthContext>();
        await context.Database.MigrateAsync();
    }
}
