using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using CustomerPortal.UserAuthService.Domain.Aggregates;
using CustomerPortal.UserAuthService.Domain.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using ThreatModel.Attributes;

namespace CustomerPortal.UserAuthService.Authentication;

public class TokenAuthenticationHandler(
    IOptionsMonitor<TokenAuthSchemeOptions> options,
    ILogger<TokenAuthenticationHandler> logger,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IUserRepository userRepository
) : AuthenticationHandler<TokenAuthSchemeOptions>(options, loggerFactory, encoder)
{
    [ThreatModelProcess("user-auth-service")]
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.TryGetValue(HeaderNames.Authorization, out var value) is false)
            return AuthenticateResult.NoResult();

        var authHeader = value.ToString();

        if (authHeader.StartsWith("Bearer ") is false)
            return AuthenticateResult.NoResult();

        var userTokenBase64 = authHeader["Bearer ".Length..];
        var userToken = Encoding.UTF8.GetString(Convert.FromBase64String(userTokenBase64));
        var split = userToken.Split(':');

        if (split.Length != 2)
        {
            logger.LogDebug("Bearer token format is invalid.");
            return AuthenticateResult.NoResult();
        }

        if (Guid.TryParse(split[0], out var userId) is false)
        {
            logger.LogDebug("User id in token is not a valid guid.");
            return AuthenticateResult.NoResult();
        }

        var token = split[1];

        var user = await userRepository.GetById(userId);

        if (user is null)
        {
            logger.LogInformation(
                "User for user with id {UserId} from token was not found.",
                userId
            );
            return AuthenticateResult.Fail("Invalid token");
        }

        if (user.IsSessionValidAt(token, DateTimeOffset.UtcNow) is false)
            return AuthenticateResult.Fail("Invalid token");

        logger.LogDebug(
            "User {UserId} authenticated from {IpAddress}.",
            user.Id,
            Context.Connection.RemoteIpAddress?.ToString()
        );

        return AuthenticateResult.Success(
            new AuthenticationTicket(CreateFromUser(user), Scheme.Name)
        );
    }

    private static ClaimsPrincipal CreateFromUser(User user)
    {
        var claimsIdentity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
            ]
        );

        return new ClaimsPrincipal(claimsIdentity);
    }
}
