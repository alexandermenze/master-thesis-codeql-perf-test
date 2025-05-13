using CustomerPortal.InternalWebsite.Configurations;
using CustomerPortal.InternalWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;
using ThreatModel.Attributes;
using ThreatModel.Tags;

namespace CustomerPortal.InternalWebsite.Pages;

public class UploadCustomerFile(
    ILogger<UploadCustomerFile> logger,
    IHttpClientFactory httpClientFactory,
    IMinioClient minio,
    MinioAppConfig minioConfig
) : UserPageModel(logger, httpClientFactory)
{
    [BindProperty]
    public int CustomerNo { get; set; }

    [BindProperty]
    public IFormFile? Upload { get; set; }

    public string? Message { get; set; }

    public void OnGet() { }

    [ThreatModelProcess("sales-dept-website")]
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var currentUser = await GetCurrentUser();

        if (currentUser is null)
            return RedirectToPage("Login");

        if (Upload is null || Upload.Length == 0)
        {
            ModelState.AddModelError(nameof(Upload), "Bitte eine Datei auswählen.");
            return Page();
        }

        var fileName = Path.GetFileName(Upload.FileName);
        var filePath = $"{CustomerNo}/{minioConfig.GenericFilesPath}/{fileName}";

        await using var stream = Upload.OpenReadStream();

        await Push(
            "upload-to-minio",
            async () =>
            {
                var putArgs = new PutObjectArgs()
                    .WithBucket(minioConfig.BucketName)
                    .WithObject(filePath)
                    .WithStreamData(stream)
                    .WithObjectSize(Upload.Length)
                    .WithContentType(Upload.ContentType);

                await minio.PutObjectAsync(putArgs);
            }
        );

        logger.LogInformation(
            "File {FilePath} was uploaded by {UserId} for customer {CustomerNo}",
            filePath,
            currentUser.Id,
            CustomerNo
        );

        Message = $"Datei {fileName} wurde erfolgreich für Nutzer {CustomerNo} hochgeladen.";
        return Page();
    }
}
