using CustomerPortal.UserAuthService.Domain.DataClasses;
using CustomerPortal.UserAuthService.Domain.Exceptions;

namespace CustomerPortal.UserAuthService.Domain.Aggregates;

public class User(Guid id, UserData userData)
{
    // Required private constructor for EntityFramework
    private User()
        : this(Guid.Empty, new UserData(null!, null!, null!, null!, UserRole.Customer)) { }

    public Guid Id { get; private set; } = id;
    public string Email { get; private set; } = userData.Email;
    public string PasswordHashWithSalt { get; private set; } = userData.PasswordHashWithSalt;
    public string FirstName { get; private set; } = userData.FirstName;
    public string LastName { get; private set; } = userData.LastName;
    public UserRole Role { get; private set; } = userData.Role;
    public UserState State { get; private set; } = UserState.Pending;
    public int? CustomerNo { get; private set; }

    private readonly List<SessionToken> _sessionTokens = [];
    public IReadOnlyList<SessionToken> SessionTokens => _sessionTokens.AsReadOnly();

    public void Approve(int customerNo)
    {
        if (State is UserState.Approved)
            throw new DomainValidationException("User is already approved.");

        State = UserState.Approved;
        CustomerNo = customerNo;
    }

    public void Deactivate()
    {
        if (State is UserState.Deactivated)
            throw new DomainValidationException("User is already deactivated.");

        _sessionTokens.Clear();
        State = UserState.Deactivated;
    }

    public SessionToken AddSessionToken(string token, DateTimeOffset expiresAt)
    {
        var sessionToken = new SessionToken(Guid.CreateVersion7(), token, expiresAt);
        _sessionTokens.Add(sessionToken);
        return sessionToken;
    }

    public bool IsSessionValidAt(string token, DateTimeOffset when)
    {
        return State is UserState.Approved
            && _sessionTokens
                .Where(s => s.Token.Equals(token, StringComparison.Ordinal))
                .Any(s => when <= s.ExpiresAt);
    }
}
