using ABC_Retail.Models;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail.Controllers
{
    public class CustomerController : Controller
    {
        private readonly TableStorageService<Customer> _table;

        public CustomerController(TableStorageService<Customer> table)
        {
            _table = table;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _table.GetAllAsync();
            return View(list.OrderBy(c => c.LastName).ToList());
        }

        public IActionResult Create() => View(new Customer());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer c)
        {
            if (!ModelState.IsValid) return View(c);
            await _table.AddAsync(c);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            var item = await _table.GetAsync(partitionKey, rowKey);
            return item is null ? NotFound() : View(item);
        }

        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var item = await _table.GetAsync(partitionKey, rowKey);
            return item is null ? NotFound() : View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer c)
        {
            if (!ModelState.IsValid) return View(c);
            await _table.UpdateAsync(c);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var item = await _table.GetAsync(partitionKey, rowKey);
            return item is null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            await _table.DeleteAsync(partitionKey, rowKey);
            return RedirectToAction(nameof(Index));
        }
    }
}
