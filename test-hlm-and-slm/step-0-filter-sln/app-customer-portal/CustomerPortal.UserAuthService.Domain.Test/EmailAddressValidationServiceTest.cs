using CustomerPortal.UserAuthService.Domain.Exceptions;
using CustomerPortal.UserAuthService.Domain.Services;

namespace CustomerPortal.UserAuthService.Domain.Test;

[TestClass]
public sealed class EmailAddressValidationServiceTest
{
    private readonly EmailAddressValidationService _service = new();

    [TestMethod]
    public void Email_WithoutAt_IsInvalid()
    {
        Assert.ThrowsException<DomainValidationException>(() => _service.EnsureIsValid("mail.de"));
    }

    [TestMethod]
    public void Email_Normal_IsValid()
    {
        _service.EnsureIsValid("mail@nothing.de");
    }
}
