using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ABC_Retail.Models;

public class ProcessOrderCommand
{
    private readonly OrderTableService _table;
    private readonly ILogger<ProcessOrderCommand> _logger;

    public ProcessOrderCommand(OrderTableService table, ILogger<ProcessOrderCommand> logger)
    {
        _table = table;
        _logger = logger;
    }

    // Queue name must match Program.cs / your storage: "order-commands"
    [Function("ProcessOrderCommand")]
    public async Task Run([QueueTrigger("order-commands", Connection = "storageConnectionString")] string message)
    {
        _logger.LogInformation("ProcessOrderCommand received: {msg}", message);

        using var doc = JsonDocument.Parse(message);
        var root = doc.RootElement;
        var action = root.GetProperty("action").GetString();

        if (string.Equals(action, "CREATE_ORDER", StringComparison.OrdinalIgnoreCase))
        {
            var p = root.GetProperty("payload");
            var order = new ABC_Retail.Models.Order
            {
                PartitionKey = p.GetProperty("partitionKey").GetString()!,
                RowKey = p.GetProperty("rowKey").GetString()!,
                CustomerRowKey = p.GetProperty("customerRowKey").GetString()!,
                ProductRowKey = p.GetProperty("productRowKey").GetString()!,
                Quantity = p.GetProperty("quantity").GetInt32(),
                Status = p.GetProperty("status").GetString()!,
                CreatedUtc = p.GetProperty("createdUtc").GetDateTime()
            };
            await _table.UpsertOrderAsync(order);
            _logger.LogInformation("Created order {rk}", order.RowKey);
        }
        else if (string.Equals(action, "UPDATE_ORDER", StringComparison.OrdinalIgnoreCase))
        {
            var p = root.GetProperty("payload");
            var partitionKey = p.GetProperty("partitionKey").GetString()!;
            var rowKey = p.GetProperty("rowKey").GetString()!;
            var status = p.GetProperty("status").GetString()!;
            await _table.UpdateStatusAsync(partitionKey, rowKey, status);
            _logger.LogInformation("Updated order {rk} to {status}", rowKey, status);
        }
        else
        {
            _logger.LogWarning("Unknown action: {action}", action);
        }
    }
}
