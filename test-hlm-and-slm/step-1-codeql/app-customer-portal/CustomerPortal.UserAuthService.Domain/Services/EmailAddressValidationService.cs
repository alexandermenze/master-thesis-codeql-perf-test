using System.ComponentModel.DataAnnotations;
using CustomerPortal.UserAuthService.Domain.Exceptions;

namespace CustomerPortal.UserAuthService.Domain.Services;

public class EmailAddressValidationService : IEmailAddressValidationService
{
    private static readonly EmailAddressAttribute Validator = new();

    public void EnsureIsValid(string email)
    {
        if (Validator.IsValid(email) is false)
            throw new DomainValidationException("Email address is not valid.");
    }
}
