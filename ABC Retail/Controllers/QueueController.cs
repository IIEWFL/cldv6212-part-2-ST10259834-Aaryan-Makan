using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail.Controllers
{
    public class QueueController : Controller
    {
        private readonly QueueStorageService _queue;
        public QueueController(QueueStorageService queue) => _queue = queue;

        public async Task<IActionResult> Index(int max = 16)
        {
            var msgs = await _queue.PeekMessagesAsync(max);
            return View(msgs);
        }
    }
}
