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

        
        var root = _share.GetRootDirectoryClient();

       
        var file = root.GetFileClient(fileName);

        
        long length;
        if (content.CanSeek)
        {
            length = content.Length;
            content.Position = 0;
        }
        else
        {
            
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

   
    public async Task UploadTextAsync(string fileName, string text)
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(text ?? string.Empty));
        await UploadFileAsync(fileName, ms);
    }
}
