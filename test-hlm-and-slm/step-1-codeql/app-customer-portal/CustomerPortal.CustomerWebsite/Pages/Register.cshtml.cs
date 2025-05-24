using CustomerPortal.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ThreatModel.Attributes;

namespace CustomerPortal.CustomerWebsite.Pages;

public class RegisterModel(IHttpClientFactory httpClientFactory) : PageModel
{
    public class InputModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("UserAuthService");

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [ThreatModelProcess("customer-website-auth")]
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var response = await Push(
            "register-as-customer",
            () => _httpClient.PostAsJsonAsync("users/register-customer", Input)
        );

        if (response.IsSuccessStatusCode)
        {
            TempData["SuccessMessage"] =
                "Ihr Account wird nun manuell geprüft und freigeschaltet. Sie erhalten eine Benachrichtigung per E-Mail.";
        }
        else
        {
            var (stringValue, problemDetails) =
                await response.Content.ReadFromJsonSafeAsync<ProblemDetails>();
            var errorMessage =
                problemDetails?.Detail ?? stringValue ?? "Es ist ein Fehler aufgetreten.";

            ModelState.AddModelError(string.Empty, errorMessage);
        }

        return Page();
    }
}
