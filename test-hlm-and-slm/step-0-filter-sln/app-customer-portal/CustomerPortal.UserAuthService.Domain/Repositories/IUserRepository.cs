using System.Collections.Immutable;
using CustomerPortal.UserAuthService.Domain.Aggregates;
using CustomerPortal.UserAuthService.Domain.DataClasses;

namespace CustomerPortal.UserAuthService.Domain.Repositories;

public interface IUserRepository
{
    Task<ImmutableArray<User>> GetAll();
    Task<ImmutableArray<User>> GetAllPendingApproval();
    Task<ImmutableArray<User>> GetByRole(UserRole role);
    Task<User?> GetById(Guid id);
    Task<User?> GetByEmail(string email);
    Task<User> Add(UserData userData);
    Task<User> Save(User user);
    Task Delete(Guid id);
}
