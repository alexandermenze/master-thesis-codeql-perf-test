using System.Collections.Immutable;
using CustomerPortal.UserAuthService.Domain.Aggregates;
using CustomerPortal.UserAuthService.Domain.DataClasses;
using CustomerPortal.UserAuthService.Domain.Exceptions;
using CustomerPortal.UserAuthService.Domain.Repositories;

namespace CustomerPortal.UserAuthService.Domain.Services;

public class RegisterUserService(
    IEmailAddressValidationService emailAddressValidationService,
    IPasswordService passwordService,
    IUserRepository userRepository
) : IRegisterUserService
{
    private static readonly ImmutableArray<UserRole> AllowedUserRoles =
    [
        UserRole.Admin,
        UserRole.SalesDepartment,
        UserRole.Customer,
    ];

    public async Task<User> Register(RegisterUserData data)
    {
        emailAddressValidationService.EnsureIsValid(data.Email);
        passwordService.EnsureRequirementsAreMet(data.Password);

        var userData = new UserData(
            data.Email,
            passwordService.HashPassword(data.Email, data.Password),
            data.FirstName,
            data.LastName,
            data.Role
        );

        if (await userRepository.GetByEmail(data.Email) is not null)
            throw new OperationConflictException("User already exists.");

        return await userRepository.Add(userData);
    }

    public Task<User> RegisterExternal(RegisterUserData data)
    {
        if (AllowedUserRoles.Contains(data.Role) is false)
            throw new DomainValidationException("Invalid user role.");

        return Register(data);
    }
}
