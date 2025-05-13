using CustomerPortal.UserAuthService.Domain.Aggregates;

namespace CustomerPortal.UserAuthService.Domain.DataClasses;

public record UserData(
    string Email,
    string PasswordHashWithSalt,
    string FirstName,
    string LastName,
    UserRole Role
);
