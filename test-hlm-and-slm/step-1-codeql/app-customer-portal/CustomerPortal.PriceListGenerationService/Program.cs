using CustomerPortal.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Minio;
using StackExchange.Redis;
using ThreatModel.Attributes;

namespace CustomerPortal.PriceListGenerationService;

public static class Program
{
    [ThreatModelProcess("pricelist-generation-service")]
    public static async Task Main()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();

        services.AddLogging(o => o.AddConsole());
        services.AddSingleton<IConnectionMultiplexer>(
            await ConnectionMultiplexer.ConnectAsync(
                config.GetValueOrThrow<string>("Redis:ConnectionString")
            )
        );

        services.AddMinio(o =>
            o.WithEndpoint(config.GetValueOrThrow<string>("MinIO:Endpoint"))
                .WithCredentials(
                    config.GetValueOrThrow<string>("MinIO:AccessKey"),
                    config.GetValueOrThrow<string>("MinIO:SecretKey")
                )
        );

        services.AddSingleton(
            new RedisConfig(
                config.GetValueOrThrow<string>("Redis:TasksStreamName"),
                config.GetValueOrThrow<string>("Redis:ConsumerGroupName")
            )
        );

        services.AddSingleton(new MinioAppConfig(config.GetValueOrThrow<string>("MinIO:Bucket")));

        services.AddTransient<App>();

        var serviceProvider = services.BuildServiceProvider();

        await serviceProvider.GetRequiredService<App>().Run(CancellationToken.None);
    }
}
