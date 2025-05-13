using CustomerPortal.UserAuthService.Domain.Aggregates;

namespace CustomerPortal.UserAuthService.Domain.DataClasses;

public record RegisterUserData(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    UserRole Role
);
