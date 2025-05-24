using System.Diagnostics;
using System.Text.Json;
using CustomerPortal.Messages.Commands;
using CustomerPortal.Messages.Events;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using StackExchange.Redis;

namespace CustomerPortal.PriceListGenerationService;

public class App(
    ILogger<App> logger,
    IConnectionMultiplexer redis,
    RedisConfig redisConfig,
    IMinioClient minioClient,
    MinioAppConfig minioAppConfig
)
{
    private readonly Guid _serviceId = Guid.CreateVersion7();

    public async Task Run(CancellationToken ct)
    {
        Push(
            "log-pricelist-generation-performance",
            () =>
                logger.LogInformation("Running PriceListGenerationService {ServiceId}", _serviceId)
        );

        await SetupConsumer();

        while (ct.IsCancellationRequested is false)
        {
            var result = await Pull(
                "get-next-pricelist-gen-task",
                () =>
                    redis
                        .GetDatabase()
                        .StreamReadGroupAsync(
                            redisConfig.TasksStreamName,
                            redisConfig.ConsumerGroupName,
                            consumerName: "price-list-generation-service",
                            position: ">",
                            count: 1
                        )
            );

            if (result.Length is not 1)
            {
                await Task.Delay(1000, ct);
                continue;
            }

            var message = result[0];

            if (message["Type"] == nameof(GenerateCustomerPriceListCommand))
            {
                var body = message["Body"];
                await ProcessMessage(body.ToString(), ct);
            }

            await Push(
                "update-task-status",
                () =>
                    redis
                        .GetDatabase()
                        .StreamAcknowledgeAsync(
                            redisConfig.TasksStreamName,
                            redisConfig.ConsumerGroupName,
                            message.Id
                        )
            );
        }

        Push(
            "log-pricelist-generation-performance",
            () =>
                logger.LogInformation("Stopped PriceListGenerationService {ServiceId}", _serviceId)
        );
    }

    private async Task SetupConsumer()
    {
        var streamExists = await redis.GetDatabase().KeyExistsAsync(redisConfig.TasksStreamName);

        var consumerExists =
            streamExists
            && (await redis.GetDatabase().StreamGroupInfoAsync(redisConfig.TasksStreamName)).Any(
                g => g.Name.Equals(redisConfig.ConsumerGroupName)
            );

        if (consumerExists)
            return;

        await redis
            .GetDatabase()
            .StreamCreateConsumerGroupAsync(
                redisConfig.TasksStreamName,
                redisConfig.ConsumerGroupName,
                "0-0"
            );

        logger.LogInformation(
            "Created consumer group {GroupName} for task stream {StreamName}",
            redisConfig.ConsumerGroupName,
            redisConfig.TasksStreamName
        );
    }

    private async Task ProcessMessage(string body, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var taskId = Guid.CreateVersion7().ToString();

        var generateCustomerPriceListCommand =
            JsonSerializer.Deserialize<GenerateCustomerPriceListCommand>(body);

        if (generateCustomerPriceListCommand is null)
            return;

        await Push(
            "update-task-status",
            () =>
                redis
                    .GetDatabase()
                    .StreamAddAsync(
                        redisConfig.TasksStreamName,
                        [
                            new NameValueEntry("TaskId", taskId),
                            new NameValueEntry(
                                "Type",
                                nameof(CustomerPriceListGenerationStartedEvent)
                            ),
                            new NameValueEntry(
                                "UserId",
                                generateCustomerPriceListCommand.UserId.ToString()
                            ),
                            new NameValueEntry(
                                "CustomerNo",
                                generateCustomerPriceListCommand.CustomerNo
                            ),
                            new NameValueEntry("CreatedAt", DateTimeOffset.UtcNow.ToString("O")),
                            new NameValueEntry(
                                "Body",
                                JsonSerializer.Serialize(
                                    new CustomerPriceListGenerationStartedEvent(
                                        Guid.CreateVersion7(),
                                        generateCustomerPriceListCommand.Id,
                                        generateCustomerPriceListCommand.SalesOrg,
                                        generateCustomerPriceListCommand.PriceDate
                                    )
                                )
                            ),
                        ]
                    )
        );

        var pdfMemoryStream = PriceListPdfGenerator.GeneratePdf(
            generateCustomerPriceListCommand.CustomerNo,
            generateCustomerPriceListCommand.SalesOrg,
            generateCustomerPriceListCommand.PriceDate
        );

        // Simulate delay
        await Task.Delay(5000, ct);

        var dateTime = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        var fileName =
            $"PriceList_{Guid.CreateVersion7():N}_{dateTime}_{generateCustomerPriceListCommand.SalesOrg}.pdf";
        var filePath = $"{generateCustomerPriceListCommand.CustomerNo}/price-lists/{fileName}";

        await Push(
            "store-generated-pricelist",
            () =>
                minioClient.PutObjectAsync(
                    new PutObjectArgs()
                        .WithStreamData(pdfMemoryStream)
                        .WithObject(filePath)
                        .WithObjectSize(pdfMemoryStream.Length)
                        .WithBucket(minioAppConfig.BucketName),
                    ct
                )
        );

        await Push(
            "update-task-status",
            () =>
                redis
                    .GetDatabase()
                    .StreamAddAsync(
                        redisConfig.TasksStreamName,
                        [
                            new NameValueEntry("TaskId", taskId),
                            new NameValueEntry("Type", nameof(CustomerPriceListGeneratedEvent)),
                            new NameValueEntry(
                                "UserId",
                                generateCustomerPriceListCommand.UserId.ToString()
                            ),
                            new NameValueEntry(
                                "CustomerNo",
                                generateCustomerPriceListCommand.CustomerNo
                            ),
                            new NameValueEntry("CreatedAt", DateTimeOffset.UtcNow.ToString("O")),
                            new NameValueEntry(
                                "Body",
                                JsonSerializer.Serialize(
                                    new CustomerPriceListGeneratedEvent(
                                        Guid.CreateVersion7(),
                                        generateCustomerPriceListCommand.Id,
                                        generateCustomerPriceListCommand.SalesOrg,
                                        generateCustomerPriceListCommand.PriceDate,
                                        filePath
                                    )
                                )
                            ),
                        ]
                    )
        );

        Push(
            "log-pricelist-generation-performance",
            () =>
                logger.LogInformation(
                    "Processed task {TaskId} in {Time}ms",
                    taskId,
                    stopwatch.ElapsedMilliseconds
                )
        );
    }
}
