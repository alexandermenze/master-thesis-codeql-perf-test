using System.Reflection;
using CustomerPortal.UserAuthService.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace CustomerPortal.UserAuthService.Postgres;

public class UserAuthContext(DbContextOptions<UserAuthContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
}
