using System.Net.Http.Headers;
using System.Text;
using CustomerPortal.Messages.Dtos;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CustomerPortal.InternalWebsite.Models;

public class UserPageModel(ILogger logger, IHttpClientFactory httpClientFactory) : PageModel
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("UserAuthService");

    protected async Task<UserResponseDto?> GetCurrentUser()
    {
        var bearerToken = User.FindFirst("BearerToken")?.Value;

        if (bearerToken is null)
            return null;

        var currentUser = await GetCurrentUser(bearerToken);

        if (currentUser?.CustomerNo is null)
            return null;

        return await GetCurrentUser(bearerToken);
    }

    protected async Task<UserResponseDto?> GetCurrentUser(string bearerToken)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                bearerToken
            );

            return await _httpClient.GetFromJsonAsync<UserResponseDto>("users/me");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get user");
            return null;
        }
    }
}
