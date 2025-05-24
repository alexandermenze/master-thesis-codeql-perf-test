using System.Text.Json;
using CustomerPortal.CustomerWebsite.Configurations;
using CustomerPortal.CustomerWebsite.Models;
using CustomerPortal.Messages.Commands;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using ThreatModel.Attributes;

namespace CustomerPortal.CustomerWebsite.Pages;

public class GeneratePriceList(
    ILogger<GeneratePriceList> logger,
    IHttpClientFactory httpClientFactory,
    IConnectionMultiplexer redis,
    RedisConfig redisConfig
) : UserPageModel(logger, httpClientFactory)
{
    [BindProperty]
    public string SalesOrg { get; set; } = string.Empty;

    [BindProperty]
    public DateOnly PriceDate { get; set; }

    public void OnGet()
    {
        PriceDate = DateOnly.FromDateTime(DateTime.Today);
    }

    [ThreatModelProcess("customer-website-core")]
    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
            return Page();

        var currentUser = await GetCurrentUser();

        if (currentUser?.CustomerNo is null)
            return RedirectToPage("/Login");

        var command = new GenerateCustomerPriceListCommand(
            Guid.CreateVersion7(),
            currentUser.Id,
            currentUser.CustomerNo.Value,
            SalesOrg,
            PriceDate
        );

        var db = redis.GetDatabase();

        var fields = new NameValueEntry[]
        {
            new("Type", nameof(GenerateCustomerPriceListCommand)),
            new("Body", JsonSerializer.Serialize(command)),
        };

        await Push(
            "create-pricelist-generation-task",
            () => db.StreamAddAsync(redisConfig.TasksStreamName, fields)
        );

        Push(
            "log-pricelist-gen-triggered",
            () =>
                logger.LogInformation(
                    "Price list generation triggered for customer {CustomerNo} by user {UserId}",
                    currentUser.CustomerNo.Value,
                    currentUser.Id
                )
        );

        return RedirectToPage("Tasks");
    }
}
