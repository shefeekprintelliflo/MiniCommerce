using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Microservice.Functions;

// POST /api/files with multipart/form-data and a file in the "file" field.
// The file is stored in a private blob container under a new GUID name, so the client's file name is never used as a path.
public class UploadFunction(BlobServiceClient blobs, ILogger<UploadFunction> logger)
{
    private const string ContainerName = "uploads";
    private const long MaxBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".jpg", ".jpeg", ".csv"
    };

    // AuthorizationLevel.Function means callers must send a function key (x-functions-key) or ?code=.
    [Function("UploadFile")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "files")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return new BadRequestObjectResult("Send the file as multipart/form-data.");
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");
        if (file is null || file.Length == 0)
        {
            return new BadRequestObjectResult("Attach a non-empty file in the 'file' field.");
        }

        if (file.Length > MaxBytes)
        {
            return new ObjectResult("The file is larger than 10 MB.") { StatusCode = StatusCodes.Status413PayloadTooLarge };
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return new BadRequestObjectResult($"File type '{extension}' is not allowed. Allowed: {string.Join(", ", AllowedExtensions)}.");
        }

        var container = blobs.GetBlobContainerClient(ContainerName);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobName = $"{Guid.NewGuid():N}{extension}";
        var blob = container.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(
            stream,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType } },
            cancellationToken);

        logger.LogInformation("Stored upload {BlobName} ({Bytes} bytes).", blobName, file.Length);

        return new CreatedResult(blob.Uri.ToString(), new { name = blobName, size = file.Length });
    }
}
