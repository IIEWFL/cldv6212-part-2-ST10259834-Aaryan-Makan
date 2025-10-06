using Azure.Storage.Blobs;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public class BlobService
{
    private readonly BlobContainerClient _container;

    public BlobService(string connectionString, string containerName)
    {
        var svc = new BlobServiceClient(connectionString?.Trim());
        _container = svc.GetBlobContainerClient(containerName.Trim());
        _container.CreateIfNotExists();
    }

    public async Task UploadTextAsync(string blobName, string content)
    {
        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("blobName is empty.");

        blobName = blobName.Trim();               // remove stray spaces/newlines
        var blob = _container.GetBlobClient(blobName);

        // Helpful debug: prints the exact URI being used.
        Console.WriteLine($"[BlobService] Uploading to: {blob.Uri}");

        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(content ?? string.Empty));
        await blob.UploadAsync(ms, overwrite: true);
    }
}
