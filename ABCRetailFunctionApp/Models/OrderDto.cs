#nullable enable
public class OrderDto
{
    public string? PartitionKey { get; set; }   // optional; default is "ORDER"
    public string? RowKey { get; set; }   // optional; if blank we generate

    public string? CustomerRowKey { get; set; }
    public string? ProductRowKey { get; set; }
    public int? Quantity { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedUtc { get; set; }
}
