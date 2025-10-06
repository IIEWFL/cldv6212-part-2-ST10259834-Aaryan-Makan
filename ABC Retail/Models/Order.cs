using Azure;
using Azure.Data.Tables;

namespace ABC_Retail.Models
{
    public class Order : ITableEntity
    {
        public string PartitionKey { get; set; } = "ORDER";
        public string RowKey { get; set; } = Guid.NewGuid().ToString("N");
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Associations
        public string CustomerRowKey { get; set; } = "";   // links to Customer.RowKey
        public string ProductRowKey { get; set; } = "";   // links to Product.RowKey

        // Business fields
        public int Quantity { get; set; } = 1;
        public string Status { get; set; } = "Pending";    // Pending | Processed | Cancelled
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
