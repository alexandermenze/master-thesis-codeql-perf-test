using CustomerPortal.UserAuthService.Domain.Aggregates;
using CustomerPortal.UserAuthService.Domain.DataClasses;

namespace CustomerPortal.UserAuthService.Domain.Factories;

public interface IUserFactory
{
    User Create(UserData userData);
}
