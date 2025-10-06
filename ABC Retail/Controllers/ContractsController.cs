using Microsoft.AspNetCore.Mvc;

// Attribution: Microsoft. (2025). Azure Tables client library for .NET. Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet (Accessed: 28 August 2025).
// Attribution: Microsoft. (2025). Get started with Azure Blob Storage and .NET. Available at: https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-dotnet-get-started (Accessed: 28 August 2025).
// Attribution: Microsoft. (2024). Tutorial: Work with Azure Queue Storage queues in .NET. Available at: https://learn.microsoft.com/en-us/azure/storage/queues/storage-tutorial-queues (Accessed: 28 August 2025).
// Attribution: Microsoft. (2025). Develop for Azure Files with .NET. Available at: https://learn.microsoft.com/en-us/azure/storage/files/storage-dotnet-how-to-use-files (Accessed: 28 August 2025).
// Attribution: Microsoft. (2025). Deploy ASP.NET Core apps to Azure App Service. Available at: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/azure-apps/?view=aspnetcore-9.0 (Accessed: 28 August 2025).
// Attribution: W3Schools. (n.d.). C# Tutorial. Available at: https://www.w3schools.com/cs/index.php (Accessed: 28 August 2025).
// Attribution: W3Schools. (n.d.). ASP.NET Razor C# Syntax. Available at: https://www.w3schools.com/asp/razor_syntax.asp (Accessed: 28 August 2025).

namespace ABC_Retail.Controllers
{
    public class ContractsController : Controller
    {
        private readonly FileShareStorageService _files;

        public ContractsController(FileShareStorageService files) => _files = files;

        public async Task<IActionResult> Index()
        {
            var names = await _files.ListFilesAsync();
            var items = names.Select(n => (Name: n, Url: _files.GetFileSasUri(n, 60))).ToList();
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file is { Length: > 0 })
            {
                using var s = file.OpenReadStream();
                await _files.UpLoadFile(file.FileName, s);
            }
            return RedirectToAction(nameof(Index));
        }

        // Optional delete (only if you added the delete button in the view)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
                await _files.DeleteFileAsync(name);
            return RedirectToAction(nameof(Index));
        }
    }
}
