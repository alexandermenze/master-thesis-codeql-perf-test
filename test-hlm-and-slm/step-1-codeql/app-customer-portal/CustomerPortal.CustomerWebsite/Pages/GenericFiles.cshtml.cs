using System.Collections.Immutable;
using CustomerPortal.CustomerWebsite.Configurations;
using CustomerPortal.CustomerWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;
using ThreatModel.Attributes;

namespace CustomerPortal.CustomerWebsite.Pages;

public class GenericFiles(
    ILogger<GenericFiles> logger,
    IHttpClientFactory httpClientFactory,
    IMinioClient minio,
    MinioAppConfig minioConfig
) : UserPageModel(logger, httpClientFactory)
{
    public ImmutableArray<string> Files = [];

    [ThreatModelProcess("customer-website-core")]
    public async Task<IActionResult> OnGet()
    {
        var currentUser = await GetCurrentUser();

        if (currentUser?.CustomerNo is null)
            return RedirectToPage("/Login");

        var filePrefix = $"{currentUser.CustomerNo}/{minioConfig.GenericFilesPath}/";

        var args = new ListObjectsArgs()
            .WithBucket(minioConfig.BucketName)
            .WithPrefix(filePrefix)
            .WithRecursive(false);

        var files = await Pull(
            "get-customer-file-list",
            () =>
                minio
                    .ListObjectsEnumAsync(args)
                    .Where(o => o.IsDir is false)
                    .Select(obj => obj.Key)
                    .ToListAsync()
        );

        var fileNames = files.Select(Path.GetFileName).Where(f => f is not null).Select(f => f!);

        Files = [.. fileNames];

        return Page();
    }

    [ThreatModelProcess("customer-website-core")]
    public async Task<IActionResult> OnPostDownloadAsync(string fileName)
    {
        // TODO: Sanitize input

        var currentUser = await GetCurrentUser();

        if (currentUser?.CustomerNo is null)
            return RedirectToPage("/Login");

        var filePrefix = $"{currentUser.CustomerNo}/{minioConfig.GenericFilesPath}/";
        var filePath = $"{filePrefix}{fileName}";

        var memoryStream = new MemoryStream();

        await Pull(
            "get-customer-file-content",
            () =>
                minio.GetObjectAsync(
                    new GetObjectArgs()
                        .WithBucket(minioConfig.BucketName)
                        .WithObject(filePath)
                        .WithCallbackStream(s => s.CopyTo(memoryStream))
                )
        );

        memoryStream.Position = 0;
        var downloadName = Path.GetFileName(filePath);

        Push(
            "log-customer-file-download",
            () =>
                logger.LogInformation(
                    "Generic file {FilePath} downloaded by user {UserId}",
                    filePath,
                    currentUser.Id
                )
        );

        return File(memoryStream, "application/octet-stream", downloadName);
    }
}
