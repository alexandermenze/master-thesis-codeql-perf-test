using CustomerPortal.Extensions;
using CustomerPortal.InternalWebsite.Configurations;
using Microsoft.AspNetCore.Authentication.Cookies;
using Minio;
using ThreatModel.Attributes;

namespace CustomerPortal.InternalWebsite;

public static class Program
{
    [ThreatModelProcess("InternalWebsite")]
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
