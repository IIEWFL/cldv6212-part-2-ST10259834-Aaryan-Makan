#nullable enable
public class OrderCommand
{
    // e.g., "CREATE_ORDER", "UPDATE_ORDER", "CANCEL_ORDER"
    public string? Action { get; set; }
    public OrderDto? Payload { get; set; }
}
