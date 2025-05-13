using CustomerPortal.Messages.Dtos;
using CustomerPortal.UserAuthService.Domain.Aggregates;

namespace CustomerPortal.UserAuthService.Domain.Extensions;

public static class UserExtensions
{
    public static UserResponseDto? ToDto(this User? user) =>
        user is null
            ? null
            : new UserResponseDto(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.Role.ToString(),
                user.State.ToString(),
                user.CustomerNo
            );
}
