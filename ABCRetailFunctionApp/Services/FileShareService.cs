using Azure.Storage.Files.Shares;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public class FileShareService
{
    private readonly ShareClient _share;

    public FileShareService(string connectionString, string shareName)
    {
        _share = new ShareClient(connectionString?.Trim(), shareName.Trim());
        _share.CreateIfNotExists(); // ok to create the SHARE
    }

    public async Task UploadFileAsync(string fileName, Stream content)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("fileName is empty.");

        fileName = fileName.Trim();

        // IMPORTANT: root directory ALREADY exists; do NOT create it.
        var root = _share.GetRootDirectoryClient();

        // Create/overwrite the file and upload content
        var file = root.GetFileClient(fileName);

        // Ensure we know length
        long length;
        if (content.CanSeek)
        {
            length = content.Length;
            content.Position = 0;
        }
        else
        {
            // buffer to memory if the stream isn't seekable
            using var ms = new MemoryStream();
            await content.CopyToAsync(ms);
            ms.Position = 0;
            length = ms.Length;
            await file.CreateAsync(length);
            await file.UploadAsync(ms);
            return;
        }

        await file.CreateAsync(length);
        await file.UploadAsync(content);
    }

    // Optional helper for text uploads (if you use it anywhere)
    public async Task UploadTextAsync(string fileName, string text)
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(text ?? string.Empty));
        await UploadFileAsync(fileName, ms);
    }
}
