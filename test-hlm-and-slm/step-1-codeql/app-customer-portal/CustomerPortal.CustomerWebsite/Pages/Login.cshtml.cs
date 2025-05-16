using System.Security.Claims;
using System.Text;
using CustomerPortal.Extensions;
using CustomerPortal.Messages.Dtos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ThreatModel.Attributes;
using ThreatModel.Tags;

namespace CustomerPortal.CustomerWebsite.Pages;

public class Login(IHttpClientFactory httpClientFactory) : PageModel
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

    [ThreatModelProcess("customer-website-auth")]
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var response = await _httpClient.PostAsJsonAsync("users/login", Input);

        if (response.IsSuccessStatusCode)
        {
            var (userId, token, expiresAt) =
                await response.Content.ReadFromJsonAsync<TokenResponseDto>()
                ?? throw new InvalidDataException("Server response is invalid.");

            var bearerToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userId}:{token}"));

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
