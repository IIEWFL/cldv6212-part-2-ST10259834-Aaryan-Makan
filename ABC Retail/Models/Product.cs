using Azure;
using Azure.Data.Tables;

namespace ABC_Retail.Models
{
    public class Product : ITableEntity
    {
        // Table keys
        public string PartitionKey { get; set; } = "PRODUCT";
        public string RowKey { get; set; } = Guid.NewGuid().ToString("N");
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Domain
        public string? ProductID { get; set; }
        public string ProductName { get; set; } = "";
        public string Description { get; set; } = "";
        public string Price { get; set; } = "";
        public string Stock { get; set; } = "";

        // Blob file name (not a URL)
        public string? ImageBlobName { get; set; }
    }
}
