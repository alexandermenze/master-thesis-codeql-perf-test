namespace CustomerPortal.Messages.Dtos;

public record RegisterCustomerUserDto(
    string Email,
    string Password,
    string FirstName,
    string LastName
);
