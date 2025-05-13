using CustomerPortal.UserAuthService.Domain.Aggregates;

namespace CustomerPortal.UserAuthService.Domain.Services;

public interface IAuthenticateUserService
{
    Task<(User, SessionToken)?> Login(string email, string password);
}
