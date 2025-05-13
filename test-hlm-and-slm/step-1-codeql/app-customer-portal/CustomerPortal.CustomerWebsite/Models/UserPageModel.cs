using System.Net.Http.Headers;
using CustomerPortal.Messages.Dtos;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CustomerPortal.CustomerWebsite.Models;

public class UserPageModel(ILogger logger, IHttpClientFactory httpClientFactory) : PageModel
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("UserAuthService");

    protected async Task<UserResponseDto?> GetCurrentUser()
    {
        var token = User.FindFirst("BearerToken")?.Value;

        if (token is null)
            return null;

        var currentUser = await GetCurrentUser(token);

        return currentUser?.CustomerNo is null ? null : currentUser;
    }

    private async Task<UserResponseDto?> GetCurrentUser(string token)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                token
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
