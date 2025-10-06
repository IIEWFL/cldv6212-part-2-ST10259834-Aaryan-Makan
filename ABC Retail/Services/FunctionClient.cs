using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public class FunctionClient
{
    private readonly HttpClient _client;
    private readonly IConfiguration _config;

    public FunctionClient(HttpClient client, IConfiguration config)
    {
        _client = client;
        _config = config;
    }

    public async Task<bool> UploadBlobAsync(string name, string content)
    {
        var url = $"{_config["FunctionEndpoints:BlobUrl"]}?name={name}";
        var response = await _client.PostAsync(url,
            new StringContent(content, Encoding.UTF8, "text/plain"));
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UploadFileAsync(string name, string content)
    {
        var url = $"{_config["FunctionEndpoints:FileUrl"]}?name={name}";
        var response = await _client.PostAsync(url,
            new StringContent(content, Encoding.UTF8, "text/plain"));
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> SendOrderCommandAsync(object payload)
    {
        var url = _config["FunctionEndpoints:QueueUrl"];
        var json = JsonSerializer.Serialize(payload);
        var response = await _client.PostAsync(url,
            new StringContent(json, Encoding.UTF8, "application/json"));
        return response.IsSuccessStatusCode;
    }
}
