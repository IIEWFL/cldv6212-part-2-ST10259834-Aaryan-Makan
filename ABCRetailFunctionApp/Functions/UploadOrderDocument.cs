using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class UploadOrderDocument
{
    private readonly BlobService _blob;
    public UploadOrderDocument(BlobService blob) => _blob = blob;

    
    [Function("UploadOrderDocument")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders/blob/{name?}")] HttpRequestData req,
        string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            name = System.Web.HttpUtility.ParseQueryString(req.Url.Query).Get("name");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("?name=<blobName> (query) or /orders/blob/{name} (route) is required.");
            return bad;
        }

        var text = await new StreamReader(req.Body).ReadToEndAsync();

        await _blob.UploadTextAsync(name, text);

        var ok = req.CreateResponse(HttpStatusCode.OK);
        await ok.WriteStringAsync($"Blob '{name}' uploaded to container 'orderdocs'.");
        return ok;
    }
}
