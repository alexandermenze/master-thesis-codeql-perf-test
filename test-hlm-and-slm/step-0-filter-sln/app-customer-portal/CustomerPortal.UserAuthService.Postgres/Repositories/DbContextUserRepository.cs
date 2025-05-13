using System.Collections.Immutable;
using CustomerPortal.UserAuthService.Domain.Aggregates;
using CustomerPortal.UserAuthService.Domain.DataClasses;
using CustomerPortal.UserAuthService.Domain.Factories;
using CustomerPortal.UserAuthService.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using ThreatModel.Attributes;

namespace CustomerPortal.UserAuthService.Postgres.Repositories;

public class DbContextUserRepository(UserAuthContext userAuthContext, IUserFactory userFactory)
    : IUserRepository
{
    public async Task<ImmutableArray<User>> GetAll() =>
        [.. await Pull("get-user-account", () => QueryUsers.ToListAsync())];

    public async Task<ImmutableArray<User>> GetAllPendingApproval() =>
        [
            .. await Pull(
                "get-user-account",
                () => QueryUsers.Where(u => u.State == UserState.Pending).ToListAsync()
            ),
        ];

    public async Task<ImmutableArray<User>> GetByRole(UserRole role) =>
        [
            .. await Pull(
                "get-user-account",
                () => QueryUsers.Where(u => u.Role == role).ToListAsync()
            ),
        ];

    public Task<User?> GetById(Guid id) =>
        Pull("get-user-account", () => QueryUsers.FirstOrDefaultAsync(x => x.Id == id));

    public Task<User?> GetByEmail(string email) =>
        Pull("get-user-account", () => QueryUsers.FirstOrDefaultAsync(x => x.Email == email));

    public async Task<User> Add(UserData userData)
    {
        return await Push(
            "insert-user-account",
            async () =>
            {
                var user = userFactory.Create(userData);
                userAuthContext.Users.Add(user);
                await userAuthContext.SaveChangesAsync();
                return user;
            }
        );
    }

    public async Task<User> Save(User user)
    {
        return await Push(
            "update-user-account",
            async () =>
            {
                if (userAuthContext.Users.Entry(user).State is EntityState.Detached)
                    throw new InvalidOperationException(
                        "User must be retrieved from the repository."
                    );

                await userAuthContext.SaveChangesAsync();
                return user;
            }
        );
    }

    public async Task Delete(Guid id)
    {
        await Push(
            "update-user-account",
            async () =>
            {
                var user = await userAuthContext.Users.FindAsync(id);

                if (user is null)
                    return;

                userAuthContext.Users.Remove(user);
                await userAuthContext.SaveChangesAsync();
            }
        );
    }

    private IQueryable<User> QueryUsers => userAuthContext.Users.Include(u => u.SessionTokens);
}
