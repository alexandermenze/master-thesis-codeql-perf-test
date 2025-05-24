using System.Collections.Immutable;
using System.Net.Http.Headers;
using System.Text.Json;
using CustomerPortal.InternalWebsite.Models;
using CustomerPortal.Messages.Dtos;
using Microsoft.AspNetCore.Mvc;
using ThreatModel.Attributes;

namespace CustomerPortal.InternalWebsite.Pages;

public class UnapprovedUsers(ILogger<UnapprovedUsers> logger, IHttpClientFactory httpClientFactory)
    : UserPageModel(logger, httpClientFactory)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("UserAuthService");

    public string ErrorMessage { get; set; } = string.Empty;
    public ImmutableArray<UserResponseDto> Users { get; private set; } = [];

    [BindProperty(Name = "ApproveCustomerNo")]
    public int ApproveCustomerNo { get; set; }

    [ThreatModelProcess("sales-dept-website")]
    public async Task<IActionResult> OnGetAsync()
    {
        var bearerToken = User.FindFirst("BearerToken")?.Value;

        if (bearerToken is null)
            return RedirectToPage("/Login");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            bearerToken
        );

        var response = await _httpClient.GetAsync("/users/unapproved");

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Users = JsonSerializer.Deserialize<ImmutableArray<UserResponseDto>>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
        }
        else
        {
            string errorMessage;

            try
            {
                errorMessage =
                    JsonSerializer
                        .Deserialize<ProblemDetails>(await response.Content.ReadAsStringAsync())
                        ?.Detail ?? "Es ist ein Fehler aufgetreten.";
            }
            catch (JsonException)
            {
                errorMessage = "Es ist ein Fehler aufgetreten.";
            }

            Users = ImmutableArray<UserResponseDto>.Empty;
            ErrorMessage = errorMessage;
        }

        return Page();
    }

    [ThreatModelProcess("sales-dept-website")]
    public async Task<IActionResult> OnPostApproveAsync(Guid id)
    {
        var bearerToken = User.FindFirst("BearerToken")?.Value;

        if (bearerToken is null)
            return RedirectToPage("/Login");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            bearerToken
        );

        var response = await Push(
            "approve-user",
            () =>
                _httpClient.PostAsJsonAsync(
                    $"/users/{id}/approve",
                    new { CustomerNo = ApproveCustomerNo }
                )
        );

        if (response.IsSuccessStatusCode)
        {
            return RedirectToPage();
        }

        ModelState.AddModelError(string.Empty, "Fehler beim Freischalten des Nutzers.");
        return await OnGetAsync();
    }
}
