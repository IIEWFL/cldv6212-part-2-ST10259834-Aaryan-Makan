using ABC_Retail.Models;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail.Controllers
{
    public class ProductController : Controller
    {
        private readonly TableStorageService<Product> _table;
        private readonly BlobStorageService _blobs;
        private readonly QueueStorageService _queue;

        public ProductController(TableStorageService<Product> table,
                                 BlobStorageService blobs,
                                 QueueStorageService queue)
        {
            _table = table;
            _blobs = blobs;
            _queue = queue;
        }

        public async Task<IActionResult> Index()
        {
            var items = await _table.GetAllAsync();
            var withUrls = new List<(Product product, string? imageUrl)>();
            foreach (var p in items)
            {
                string? url = null;
                if (!string.IsNullOrWhiteSpace(p.ImageBlobName))
                    url = _blobs.GetImageSasUri(p.ImageBlobName, 60);
                withUrls.Add((p, url));
            }
            return View(withUrls);
        }

        public IActionResult Create() => View(new Product());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product p, IFormFile? image)
        {
            if (!ModelState.IsValid) return View(p);

            if (image is not null && image.Length > 0)
            {
                var blobName = $"{p.RowKey}-{Path.GetFileName(image.FileName)}";
                using var stream = image.OpenReadStream();
                await _blobs.UploadImageAsync(stream, blobName);
                p.ImageBlobName = blobName;
            }

            await _table.AddAsync(p);
            await _queue.SendMessagesAsync(new { action = "CREATE_PRODUCT", id = p.RowKey, when = DateTime.UtcNow });

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            var p = await _table.GetAsync(partitionKey, rowKey);
            if (p is null) return NotFound();
            ViewBag.ImageUrl = !string.IsNullOrEmpty(p.ImageBlobName) ? _blobs.GetImageSasUri(p.ImageBlobName, 60) : null;
            return View(p);
        }

        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var p = await _table.GetAsync(partitionKey, rowKey);
            return p is null ? NotFound() : View(p);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product p, IFormFile? image)
        {
            if (!ModelState.IsValid) return View(p);

            if (image is not null && image.Length > 0)
            {
                var blobName = $"{p.RowKey}-{Path.GetFileName(image.FileName)}";
                using var stream = image.OpenReadStream();
                await _blobs.UploadImageAsync(stream, blobName);
                p.ImageBlobName = blobName;
            }

            await _table.UpdateAsync(p);
            await _queue.SendMessagesAsync(new { action = "UPDATE_PRODUCT", id = p.RowKey, when = DateTime.UtcNow });

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var p = await _table.GetAsync(partitionKey, rowKey);
            return p is null ? NotFound() : View(p);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            await _table.DeleteAsync(partitionKey, rowKey);
            await _queue.SendMessagesAsync(new { action = "DELETE_PRODUCT", id = rowKey, when = DateTime.UtcNow });
            return RedirectToAction(nameof(Index));
        }
    }
}
