using CustomerPortal.UserAuthService.Domain.Aggregates;
using CustomerPortal.UserAuthService.Domain.Repositories;

namespace CustomerPortal.UserAuthService.Domain.Services;

public class AuthenticateUserService(
    IUserRepository userRepository,
    IPasswordService passwordService,
    ITokenGenerationService tokenGenerationService,
    TimeProvider timeProvider
) : IAuthenticateUserService
{
    public async Task<(User, SessionToken)?> Login(string email, string password)
    {
        var user = await userRepository.GetByEmail(email);

        if (user?.State is not UserState.Approved)
            return null;

        if (
            passwordService.VerifyPassword(user.Email, user.PasswordHashWithSalt, password) is false
        )
            return null;

        var token = tokenGenerationService.Generate();

        var sessionToken = user.AddSessionToken(token, timeProvider.GetUtcNow().AddHours(1));

        await userRepository.Save(user);

        return (user, sessionToken);
    }
}
