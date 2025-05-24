using System.Security.Claims;
using System.Text;
using CustomerPortal.Extensions;
using CustomerPortal.InternalWebsite.Models;
using CustomerPortal.Messages.Dtos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using ThreatModel.Attributes;

namespace CustomerPortal.InternalWebsite.Pages;

public class Login(ILogger<Login> logger, IHttpClientFactory httpClientFactory)
    : UserPageModel(logger, httpClientFactory)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("UserAuthService");

    [BindProperty]
    public InputModel? Input { get; set; }

    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    [ThreatModelProcess("sales-dept-website")]
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var response = await Push(
            "authenticate-internal-user",
            () => _httpClient.PostAsJsonAsync("users/login", Input)
        );

        if (response.IsSuccessStatusCode)
        {
            var (userId, token, expiresAt) =
                await response.Content.ReadFromJsonAsync<TokenResponseDto>()
                ?? throw new InvalidDataException("Server response is invalid.");

            var bearerToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userId}:{token}"));

            var user = await GetCurrentUser(bearerToken);

            if (user is null || user.Role.Equals("Customer", StringComparison.Ordinal))
                return RedirectToPage();

            var claims = new List<Claim> { new("BearerToken", bearerToken) };
            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = true, ExpiresUtc = expiresAt }
            );

            return RedirectToPage("/Index");
        }
        else
        {
            var (stringValue, problemDetails) =
                await response.Content.ReadFromJsonSafeAsync<ProblemDetails>();
            var errorMessage =
                problemDetails?.Detail ?? stringValue ?? "Login war nicht erfolgreich.";

            ModelState.AddModelError(string.Empty, errorMessage);
            return Page();
        }
    }
}
