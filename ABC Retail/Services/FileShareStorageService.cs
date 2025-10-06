using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Azure.Storage.Sas;

public class FileShareStorageService
{
    private readonly ShareClient _shareClient;

    public FileShareStorageService(string storageConnectionString, string shareName)
    {
        var service = new ShareServiceClient(storageConnectionString);
        _shareClient = service.GetShareClient(shareName);

        // This creates the *share* (and its root) if needed.
        // Do NOT try to create the root directory explicitly.
        _shareClient.CreateIfNotExists();
    }

    // Upload a file into the root directory
    public async Task UpLoadFile(string fileName, Stream fileStream)
    {
        var dir = _shareClient.GetRootDirectoryClient(); // root already exists
        var file = dir.GetFileClient(fileName);

        // Create the file with the correct length, then upload ranges
        await file.CreateAsync(fileStream.Length);

        const int chunk = 4 * 1024 * 1024; // 4 MB
        long pos = 0;
        byte[] buffer = new byte[chunk];
        int read;
        while ((read = await fileStream.ReadAsync(buffer, 0, chunk)) > 0)
        {
            using var ms = new MemoryStream(buffer, 0, read);
            await file.UploadRangeAsync(
                ShareFileRangeWriteType.Update,
                new HttpRange(pos, read),
                ms
            );
            pos += read;
        }
    }

    // List files in the root directory
    public async Task<List<string>> ListFilesAsync()
    {
        var dir = _shareClient.GetRootDirectoryClient(); // root already exists
        var names = new List<string>();

        await foreach (ShareFileItem item in dir.GetFilesAndDirectoriesAsync())
        {
            if (!item.IsDirectory) names.Add(item.Name);
        }
        return names;
    }

    // Optional: delete a file (useful if you added the delete button)
    public async Task<bool> DeleteFileAsync(string fileName)
    {
        var dir = _shareClient.GetRootDirectoryClient();
        var file = dir.GetFileClient(fileName);
        var resp = await file.DeleteIfExistsAsync();
        return resp.Value;
    }

    // Generate a read-only SAS link to download the file
    public string GetFileSasUri(string fileName, int validMinutes = 60)
    {
        var dir = _shareClient.GetRootDirectoryClient();
        var file = dir.GetFileClient(fileName);

        if (!file.CanGenerateSasUri)
            throw new InvalidOperationException(
                "Cannot generate SAS from FileClient. Ensure your connection string uses an account key (not a SAS).");

        var sas = new ShareSasBuilder
        {
            ShareName = _shareClient.Name,
            FilePath = fileName,
            Resource = "f",
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(validMinutes)
        };
        sas.SetPermissions(ShareFileSasPermissions.Read);

        return file.GenerateSasUri(sas).ToString();
    }
}
