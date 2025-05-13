using System.Security.Cryptography;
using CustomerPortal.UserAuthService.Domain.Services;

namespace CustomerPortal.UserAuthService.Services;

public class TokenGenerationService : ITokenGenerationService
{
    public string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
