using System.Collections.Immutable;
using System.Text.Json;
using CustomerPortal.CustomerWebsite.Configurations;
using CustomerPortal.CustomerWebsite.Models;
using CustomerPortal.Messages.Events;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;
using StackExchange.Redis;
using ThreatModel.Attributes;

namespace CustomerPortal.CustomerWebsite.Pages;

public class PriceLists(
    ILogger<PriceLists> logger,
    IHttpClientFactory httpClientFactory,
    IConnectionMultiplexer redis,
    RedisConfig redisConfig,
    IMinioClient minio,
    MinioAppConfig minioConfig
) : UserPageModel(logger, httpClientFactory)
{
    public class TaskViewModel
    {
        public Guid Id { get; set; }
        public DateTimeOffset FirstCreatedAt { get; set; }
        public string? LastDescription { get; set; }
        public bool HasDownload { get; set; }
    }

    public ImmutableArray<TaskViewModel> TaskViewModels { get; set; } =
        ImmutableArray<TaskViewModel>.Empty;

    [ThreatModelProcess("customer-website-core")]
    public async Task<IActionResult> OnGet()
    {
        var currentUser = await GetCurrentUser();

        if (currentUser?.CustomerNo is null)
            return RedirectToPage("/Login");

        var userTasks = await GetUserTasks(currentUser.CustomerNo.Value);

        TaskViewModels =
        [
            .. userTasks
                .Values.Select(ts =>
                {
                    var first = ts.History.OrderBy(h => h.DateTime).First().DateTime;

                    var lastDesc = ts
                        .History.OrderByDescending(h => h.DateTime)
                        .First()
                        .Description;

                    var latestLink = ts
                        .History.Where(h => !string.IsNullOrEmpty(h.FileDownloadLink))
                        .OrderByDescending(h => h.DateTime)
                        .Select(h => h.FileDownloadLink)
                        .FirstOrDefault();

                    return new TaskViewModel
                    {
                        Id = ts.Id,
                        FirstCreatedAt = first,
                        LastDescription = lastDesc,
                        HasDownload = latestLink is not null,
                    };
                })
                .OrderByDescending(vm => vm.FirstCreatedAt),
        ];

        return Page();
    }

    [ThreatModelProcess("customer-website-core")]
    public async Task<IActionResult> OnPostDownload(Guid id)
    {
        var currentUser = await GetCurrentUser();

        if (currentUser?.CustomerNo is null)
            return RedirectToPage("/Login");

        var userTasks = await GetUserTasks(currentUser.CustomerNo.Value);

        var filePath = userTasks[id]
            .History.OrderByDescending(t => t.DateTime)
            .Last(t => t.FileDownloadLink is not null)
            .FileDownloadLink;

        var memoryStream = new MemoryStream();

        await minio.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket(minioConfig.BucketName)
                .WithObject(filePath)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream))
        );

        var fileName = Path.GetFileName(filePath);

        memoryStream.Position = 0;
        const string contentType = "application/pdf";

        logger.LogInformation(
            "Price list {FilePath} downloaded by user {UserId}",
            filePath,
            currentUser.Id
        );

        return File(memoryStream, contentType, fileName);
    }

    private async Task<Dictionary<Guid, TaskStatus>> GetUserTasks(int customerNo)
    {
        var entries = await redis.GetDatabase().StreamReadAsync(redisConfig.TasksStreamName, "0-0");

        var state = new Dictionary<Guid, TaskStatus>();

        foreach (var entry in entries)
        {
            if (entry["CustomerNo"] != customerNo)
                continue;

            var taskId = Guid.TryParse(entry["TaskId"], out var id) ? (Guid?)id : null;

            if (taskId is null)
                continue;

            var entryType = entry["Type"];

            if (entryType.HasValue is false)
                continue;

            var taskStatusChange = entryType.ToString() switch
            {
                nameof(CustomerPriceListGenerationStartedEvent) =>
                    MapFromCustomerPriceListGenerationStartedEvent(entry),
                nameof(CustomerPriceListGeneratedEvent) => MapFromCustomerPriceListGeneratedEvent(
                    entry
                ),
                _ => null,
            };

            if (taskStatusChange is null)
                continue;

            var taskState =
                state.GetValueOrDefault(taskId.Value) ?? new TaskStatus(taskId.Value, []);

            taskState = taskState with { History = taskState.History.Add(taskStatusChange) };

            state[taskId.Value] = taskState;
        }

        return state;
    }

    private static TaskStatusChange MapFromCustomerPriceListGenerationStartedEvent(
        StreamEntry entry
    )
    {
        var createdAt = DateTimeOffset.Parse(entry["CreatedAt"].ToString());

        var body =
            JsonSerializer.Deserialize<CustomerPriceListGenerationStartedEvent>(
                entry["Body"].ToString()
            ) ?? throw new InvalidOperationException("Invalid body.");

        var description =
            $"Price list generation started for Sales Org {body.SalesOrg} with Price Date {body.PriceDate}.";

        return new TaskStatusChange(createdAt, description, null);
    }

    private static TaskStatusChange MapFromCustomerPriceListGeneratedEvent(StreamEntry entry)
    {
        var createdAt = DateTimeOffset.Parse(entry["CreatedAt"].ToString());

        var body =
            JsonSerializer.Deserialize<CustomerPriceListGeneratedEvent>(entry["Body"].ToString())
            ?? throw new InvalidOperationException("Invalid body.");

        var description =
            $"Price list generated for Sales Org {body.SalesOrg} with Price Date {body.PriceDate}.";

        return new TaskStatusChange(createdAt, description, body.StorageFilePath);
    }

    private record TaskStatus(Guid Id, ImmutableArray<TaskStatusChange> History);

    private record TaskStatusChange(
        DateTimeOffset DateTime,
        string Description,
        string? FileDownloadLink
    );
}
