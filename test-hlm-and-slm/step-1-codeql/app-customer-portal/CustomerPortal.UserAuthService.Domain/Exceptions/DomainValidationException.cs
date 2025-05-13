namespace CustomerPortal.UserAuthService.Domain.Exceptions;

public class DomainValidationException : Exception
{
    public DomainValidationException(string message, Exception inner)
        : base(message, inner) { }

    public DomainValidationException(string message)
        : base(message) { }

    public DomainValidationException() { }
}
