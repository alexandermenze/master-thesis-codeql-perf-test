using CustomerPortal.UserAuthService.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CustomerPortal.UserAuthService.Domain.Services;

public class PasswordService(
    ILogger<PasswordService> logger,
    IPasswordHasher<string> passwordHasher
) : IPasswordService
{
    public void EnsureRequirementsAreMet(string password)
    {
        if (password.Length < 12)
            throw new DomainValidationException("Password must be at least 12 characters.");
    }

    public string HashPassword(string email, string password) =>
        passwordHasher.HashPassword(email, password);

    public bool VerifyPassword(string email, string hashedPassword, string password)
    {
        return passwordHasher.VerifyHashedPassword(email, hashedPassword, password) switch
        {
            PasswordVerificationResult.Success => true,
            PasswordVerificationResult.SuccessRehashNeeded => LogAndAccept(),
            _ => false,
        };

        bool LogAndAccept()
        {
            Push(
                "log-user-auth-events",
                () =>
                    logger.LogWarning(
                        "Outdated password hashing algorithm detected. Implement rehash."
                    )
            );
            return true;
        }
    }
}
