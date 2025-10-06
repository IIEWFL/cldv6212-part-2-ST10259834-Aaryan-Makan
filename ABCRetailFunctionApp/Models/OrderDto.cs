#nullable enable
public class OrderDto
{
    public string? PartitionKey { get; set; }   
    public string? RowKey { get; set; }  

    public string? CustomerRowKey { get; set; }
    public string? ProductRowKey { get; set; }
    public int? Quantity { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedUtc { get; set; }
}
