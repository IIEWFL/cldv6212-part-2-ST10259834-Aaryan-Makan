using System.Net;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class SaveReportToFileShare
{
    private readonly FileShareService _files;
    public SaveReportToFileShare(FileShareService files) => _files = files;

    
    [Function("SaveReportToFileShare")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders/file/{name?}")] HttpRequestData req,
        string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            name = System.Web.HttpUtility.ParseQueryString(req.Url.Query).Get("name");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("?name=<fileName> (query) or /orders/file/{name} (route) is required.");
            return bad;
        }

        var text = await new StreamReader(req.Body).ReadToEndAsync();
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(text));
        await _files.UploadFileAsync(name, ms);

        var ok = req.CreateResponse(HttpStatusCode.OK);
        await ok.WriteStringAsync($"File '{name}' uploaded to share 'orderfiles'.");
        return ok;
    }
}
