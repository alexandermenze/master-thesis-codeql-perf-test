using CustomerPortal.CustomerWebsite.Configurations;
using CustomerPortal.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Minio;
using StackExchange.Redis;
using ThreatModel.Attributes;

namespace CustomerPortal.CustomerWebsite;

public static class Program
{
    [ThreatModelProcess("CustomerWebsite")]
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddHttpClient(
            "UserAuthService",
            o =>
                o.BaseAddress = new Uri(
                    builder.Configuration.GetValueOrThrow<string>("UserAuthService:BaseUrl")
                )
        );

        builder
            .Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "Cookies";
                options.LoginPath = "/Login";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
            });

        builder.Services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(
                builder.Configuration.GetValueOrThrow<string>("Redis:ConnectionString")
            )
        );

        builder.Services.AddSingleton(
            new RedisConfig(builder.Configuration.GetValueOrThrow<string>("Redis:TasksStreamName"))
        );

        builder.Services.AddMinio(o =>
        {
            o.WithEndpoint(builder.Configuration.GetValueOrThrow<string>("MinIO:Endpoint"))
                .WithCredentials(
                    builder.Configuration.GetValueOrThrow<string>("MinIO:AccessKey"),
                    builder.Configuration.GetValueOrThrow<string>("MinIO:SecretKey")
                )
                .WithSSL(false);
        });

        builder.Services.AddSingleton(
            new MinioAppConfig(
                builder.Configuration.GetValueOrThrow<string>("MinIO:BucketName"),
                builder.Configuration.GetValueOrThrow<string>("MinIO:GenericFilesPath")
            )
        );

        builder.Services.AddRazorPages();

        var app = builder.Build();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapStaticAssets();
        app.MapRazorPages().WithStaticAssets();

        await app.RunAsync();
    }
}
