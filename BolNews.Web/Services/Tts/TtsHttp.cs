using System.Net;
using System.Net.Http.Headers;

namespace BolNews.Web.Services.Tts;

public static class TtsHttp
{
    public const string ClientName = "tts";

    /// <summary>Sends the request and turns provider errors into a short, key-free message.</summary>
    public static async Task<HttpResponseMessage> SendAsync(IHttpClientFactory factory, HttpRequestMessage request, string provider, CancellationToken ct)
    {
        HttpResponseMessage response;
        try
        {
            response = await factory.CreateClient(ClientName).SendAsync(request, ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new TtsException($"{provider} did not answer in time.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new TtsException($"{provider} could not be reached: {ex.Message}", ex);
        }

        if (response.IsSuccessStatusCode) return response;

        var body = await response.Content.ReadAsStringAsync(ct);
        response.Dispose();
        if (body.Length > 300) body = body[..300];
        var hint = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => " Check the API key and its permissions.",
            HttpStatusCode.TooManyRequests => " The provider rate limit or quota was reached.",
            HttpStatusCode.NotFound => " Check the voice name/ID, model and region.",
            HttpStatusCode.BadRequest => " Check the voice name/ID and model.",
            _ => ""
        };
        throw new TtsException($"{provider} returned {(int)response.StatusCode} {response.ReasonPhrase}.{hint} {body}".Trim());
    }

    public static AuthenticationHeaderValue Bearer(string key) => new("Bearer", key);
}
