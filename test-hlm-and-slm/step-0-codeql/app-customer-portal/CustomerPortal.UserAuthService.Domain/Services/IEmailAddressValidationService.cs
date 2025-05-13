namespace CustomerPortal.UserAuthService.Domain.Services;

public interface IEmailAddressValidationService
{
    void EnsureIsValid(string email);
}
