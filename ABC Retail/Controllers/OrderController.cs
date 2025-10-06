using ABC_Retail.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ABC_Retail.Controllers
{
    public class OrderController : Controller
    {
        private readonly TableStorageService<Order> _orders;
        private readonly TableStorageService<Customer> _customers;
        private readonly TableStorageService<Product> _products;
        private readonly QueueStorageService _queue;

        public OrderController(
            TableStorageService<Order> orders,
            TableStorageService<Customer> customers,
            TableStorageService<Product> products,
            QueueStorageService queue)
        {
            _orders = orders;
            _customers = customers;
            _products = products;
            _queue = queue;
        }

        // Helper for dropdowns
        private async Task PopulateDropDownsAsync(string? selectedCustomer = null, string? selectedProduct = null)
        {
            var customers = (await _customers.GetAllAsync())
                .Select(c => new { Id = c.RowKey, Name = $"{c.FirstName} {c.LastName}" })
                .OrderBy(x => x.Name)
                .ToList();

            var products = (await _products.GetAllAsync())
                .Select(p => new { Id = p.RowKey, Name = p.ProductName })
                .OrderBy(x => x.Name)
                .ToList();

            ViewBag.CustomerOptions = new SelectList(customers, "Id", "Name", selectedCustomer);
            ViewBag.ProductOptions = new SelectList(products, "Id", "Name", selectedProduct);
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _orders.GetAllAsync();
            var customers = (await _customers.GetAllAsync()).ToDictionary(c => c.RowKey, c => $"{c.FirstName} {c.LastName}");
            var products = (await _products.GetAllAsync()).ToDictionary(p => p.RowKey, p => p.ProductName);

            var vm = orders
                .OrderByDescending(o => o.CreatedUtc)
                .Select(o => new OrderListItem
                {
                    Order = o,
                    CustomerName = customers.TryGetValue(o.CustomerRowKey, out var cn) ? cn : o.CustomerRowKey,
                    ProductName = products.TryGetValue(o.ProductRowKey, out var pn) ? pn : o.ProductRowKey
                })
                .ToList();

            return View(vm);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateDropDownsAsync();
            return View(new Order());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order o)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropDownsAsync(o.CustomerRowKey, o.ProductRowKey);
                return View(o);
            }

            await _orders.AddAsync(o);
            await _queue.SendMessagesAsync(new
            {
                action = "PROCESS_ORDER",
                orderId = o.RowKey,
                customerId = o.CustomerRowKey,
                productId = o.ProductRowKey,
                quantity = o.Quantity,
                status = o.Status,
                when = DateTime.UtcNow
            });

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            var o = await _orders.GetAsync(partitionKey, rowKey);
            if (o is null) return NotFound();

            var c = await _customers.GetAsync("CUSTOMER", o.CustomerRowKey);
            var p = await _products.GetAsync("PRODUCT", o.ProductRowKey);
            ViewBag.CustomerName = c is null ? o.CustomerRowKey : $"{c.FirstName} {c.LastName}";
            ViewBag.ProductName = p is null ? o.ProductRowKey : p.ProductName;

            return View(o);
        }

        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var o = await _orders.GetAsync(partitionKey, rowKey);
            if (o is null) return NotFound();
            await PopulateDropDownsAsync(o.CustomerRowKey, o.ProductRowKey);
            return View(o);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order o)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropDownsAsync(o.CustomerRowKey, o.ProductRowKey);
                return View(o);
            }

            await _orders.UpdateAsync(o);
            await _queue.SendMessagesAsync(new
            {
                action = "UPDATE_ORDER",
                orderId = o.RowKey,
                customerId = o.CustomerRowKey,
                productId = o.ProductRowKey,
                quantity = o.Quantity,
                status = o.Status,
                when = DateTime.UtcNow
            });

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var o = await _orders.GetAsync(partitionKey, rowKey);
            if (o is null) return NotFound();
            return View(o);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            await _orders.DeleteAsync(partitionKey, rowKey);
            await _queue.SendMessagesAsync(new
            {
                action = "CANCEL_ORDER",
                orderId = rowKey,
                when = DateTime.UtcNow
            });
            return RedirectToAction(nameof(Index));
        }
    }

    // Simple view model for the Index list
    public class OrderListItem
    {
        public Order Order { get; set; } = default!;
        public string CustomerName { get; set; } = "";
        public string ProductName { get; set; } = "";
    }
}
