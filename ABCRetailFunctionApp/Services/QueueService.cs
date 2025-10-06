using Azure.Storage.Queues;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

public class QueueService
{
    private readonly QueueClient _queue;

    public QueueService(string connectionString, string queueName)
    {
        var svc = new QueueServiceClient(connectionString?.Trim());
        _queue = svc.GetQueueClient(queueName.Trim());
        _queue.CreateIfNotExists();
    }

    // 1) Raw JSON overload (no extra serialization)
    public Task SendAsync(string json) =>
        _queue.SendMessageAsync(json);

    // 2) Object overload (serialize to JSON once)
    public Task SendAsync(object messageJsonObject) =>
        _queue.SendMessageAsync(JsonSerializer.Serialize(messageJsonObject));

    public async Task<List<string>> PeekAsync(int max = 16)
    {
        var list = new List<string>();
        var msgs = (await _queue.PeekMessagesAsync(max)).Value;
        foreach (var m in msgs) list.Add(m.MessageText);
        return list;
    }

    public Task ClearAsync() => _queue.ClearMessagesAsync();
}
