using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class EnqueueOrderCommand
{
    private readonly QueueService _queue;
    public EnqueueOrderCommand(QueueService queue) => _queue = queue;

    // Route: /api/orders/command   (singular "command")
    [Function("EnqueueOrderCommand")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders/command")] HttpRequestData req)
    {
        var json = await new StreamReader(req.Body).ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(json))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("JSON body is required.");
            return bad;
        }

        await _queue.SendAsync(json);  // raw JSON, no re-serialization
        var resp = req.CreateResponse(HttpStatusCode.Accepted);
        await resp.WriteStringAsync("Command enqueued.");
        return resp;
    }
}
