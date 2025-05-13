using CustomerPortal.UserAuthService.Domain.Aggregates;
using CustomerPortal.UserAuthService.Domain.DataClasses;

namespace CustomerPortal.UserAuthService.Domain.Factories;

public class UserFactory : IUserFactory
{
    public User Create(UserData userData) => new(Guid.CreateVersion7(), userData);
}
