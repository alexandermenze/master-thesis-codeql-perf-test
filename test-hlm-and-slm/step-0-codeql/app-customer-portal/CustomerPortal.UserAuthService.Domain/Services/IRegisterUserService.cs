using CustomerPortal.UserAuthService.Domain.Aggregates;
using CustomerPortal.UserAuthService.Domain.DataClasses;

namespace CustomerPortal.UserAuthService.Domain.Services;

public interface IRegisterUserService
{
    Task<User> Register(RegisterUserData data);
    Task<User> RegisterExternal(RegisterUserData data);
}
