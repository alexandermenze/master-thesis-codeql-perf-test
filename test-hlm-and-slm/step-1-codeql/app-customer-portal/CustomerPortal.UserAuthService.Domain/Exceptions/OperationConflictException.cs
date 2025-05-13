namespace CustomerPortal.UserAuthService.Domain.Exceptions;

public class OperationConflictException : Exception
{
    public OperationConflictException(string message, Exception inner)
        : base(message, inner) { }

    public OperationConflictException(string message)
        : base(message) { }

    public OperationConflictException() { }
}
