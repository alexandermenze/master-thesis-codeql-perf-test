namespace CustomerPortal.Messages.Dtos;

public record TokenResponseDto(Guid UserId, string Token, DateTimeOffset ExpiresAt);
