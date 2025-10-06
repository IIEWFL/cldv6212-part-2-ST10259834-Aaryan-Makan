using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System.Text;

public class QueueStorageService
{
    private readonly QueueClient _queueClient;

    public QueueStorageService(string storageConnectionString, string queueName)
    {
        var serviceClient = new QueueServiceClient(storageConnectionString);
        _queueClient = serviceClient.GetQueueClient(queueName);
        _queueClient.CreateIfNotExists();
    }

    public async Task SendMessagesAsync(object message)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(message);
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        await _queueClient.SendMessageAsync(base64);
    }

    // Peek (non-destructive) so you can show messages in the app
    public async Task<List<string>> PeekMessagesAsync(int maxMessages = 16)
    {
        var results = new List<string>();
        PeekedMessage[] peeked = (await _queueClient.PeekMessagesAsync(maxMessages)).Value;

        foreach (var m in peeked)
        {
            try
            {
                // we stored base64 text; decode back to JSON
                var bytes = Convert.FromBase64String(m.MessageText);
                results.Add(Encoding.UTF8.GetString(bytes));
            }
            catch
            {
                // if not base64, just show as-is
                results.Add(m.MessageText);
            }
        }
        return results;
    }
}
