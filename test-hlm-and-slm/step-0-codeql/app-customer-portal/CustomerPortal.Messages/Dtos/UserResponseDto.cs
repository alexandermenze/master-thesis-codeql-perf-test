namespace CustomerPortal.Messages.Dtos;

public record UserResponseDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    string State,
    int? CustomerNo
);
