using System;
using System.Linq;
using System.Threading.Tasks;
using ABC_Retail.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;



namespace ABC_Retail.Controllers
{
    public class OrderController : Controller
    {
        private readonly TableStorageService<Order> _orders;      // READ-ONLY
        private readonly TableStorageService<Customer> _customers; // READ-ONLY
        private readonly TableStorageService<Product> _products;   // READ-ONLY
        private readonly QueueStorageService _queue;               // kept to avoid DI issues (no longer used)
        private readonly FunctionClient _func;                     // writes go via Function App

        public OrderController(
            TableStorageService<Order> orders,
            TableStorageService<Customer> customers,
            TableStorageService<Product> products,
            QueueStorageService queue,
            FunctionClient func)
        {
            _orders = orders;
            _customers = customers;
            _products = products;
            _queue = queue; // not used now; retained for DI stability
            _func = func;
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

        // INDEX with simple search (by RowKey, CustomerRowKey, or Status)
        public async Task<IActionResult> Index(string? q)
        {
            var orders = await _orders.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var search = q.Trim().ToLowerInvariant();
                orders = orders.Where(o =>
                    (o.RowKey ?? "").ToLowerInvariant().Contains(search) ||
                    (o.CustomerRowKey ?? "").ToLowerInvariant().Contains(search) ||
                    (o.Status ?? "").ToLowerInvariant().Contains(search)
                ).ToList();
            }

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

            ViewBag.Query = q;
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

            // Queue CREATE (do NOT write tables directly)
            if (string.IsNullOrWhiteSpace(o.RowKey))
                o.RowKey = "ORD-" + Guid.NewGuid().ToString("N")[..6];

            var cmd = new
            {
                action = "CREATE_ORDER",
                payload = new
                {
                    partitionKey = "ORDER",
                    rowKey = o.RowKey,
                    customerRowKey = o.CustomerRowKey,
                    productRowKey = o.ProductRowKey,
                    quantity = o.Quantity,
                    status = string.IsNullOrWhiteSpace(o.Status) ? "Pending" : o.Status,
                    createdUtc = DateTime.UtcNow
                }
            };

            var ok = await _func.SendOrderCommandAsync(cmd);
            TempData["msg"] = ok ? $"Order {o.RowKey} queued for creation." : "Failed to queue order.";
            return RedirectToAction(nameof(Index), new { q = o.RowKey });
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

            // Queue UPDATE (status)
            var cmd = new
            {
                action = "UPDATE_ORDER",
                payload = new
                {
                    partitionKey = "ORDER",
                    rowKey = o.RowKey,
                    status = string.IsNullOrWhiteSpace(o.Status) ? "Pending" : o.Status
                }
            };

            var ok = await _func.SendOrderCommandAsync(cmd);
            TempData["msg"] = ok ? $"Order {o.RowKey} status queued for update." : "Failed to queue status update.";
            return RedirectToAction(nameof(Index), new { q = o.RowKey });
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
            // Queue UPDATE to mark as Cancelled (no direct delete)
            var cmd = new
            {
                action = "UPDATE_ORDER",
                payload = new { partitionKey = "ORDER", rowKey, status = "Cancelled" }
            };

            var ok = await _func.SendOrderCommandAsync(cmd);
            TempData["msg"] = ok ? $"Order {rowKey} queued to be cancelled." : "Failed to queue cancel request.";
            return RedirectToAction(nameof(Index), new { q = rowKey });
        }

        // NEW: inline Status update endpoint for the grid dropdown
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(string rowKey, string status)
        {
            var cmd = new
            {
                action = "UPDATE_ORDER",
                payload = new { partitionKey = "ORDER", rowKey, status }
            };

            var ok = await _func.SendOrderCommandAsync(cmd);
            TempData["msg"] = ok ? $"Order {rowKey} queued to {status}." : "Failed to queue status change.";
            return RedirectToAction(nameof(Index), new { q = rowKey });
        }
    }

    // View model for the Index list
    public class OrderListItem
    {
        public Order Order { get; set; } = default!;
        public string CustomerName { get; set; } = "";
        public string ProductName { get; set; } = "";
    }
}
