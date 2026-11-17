using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace GenAIWorker;

public class GeminiOptions
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gemini-3.8-flash";
}

public class GeminiException(string message, bool isTransient) : Exception(message)
{
    public bool IsTransient { get; } = isTransient;
}

public class GeminiClient(HttpClient http, IOptions<GeminiOptions> options, ILogger<GeminiClient> log)
{
    private const int MaxChars = 30_000;

    public async Task<string> SummarizeAsync(string text, CancellationToken ct)
    {
        var delays = new[] { TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) };

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await SummarizeOnceAsync(text, ct);
            }
            catch (GeminiException ex) when (ex.IsTransient && attempt < delays.Length)
            {
                log.LogWarning(ex, "Gemini temporarily unavailable, retry {Attempt}/{Max} in {Delay}s",
                    attempt + 1, delays.Length, delays[attempt].TotalSeconds);
                await Task.Delay(delays[attempt], ct);
            }
        }
    }

    private async Task<string> SummarizeOnceAsync(string text, CancellationToken ct)
    {
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.ApiKey) || o.ApiKey == "changeme")
            throw new GeminiException("Gemini API key is not configured", false);

        if (text.Length > MaxChars)
        {
            log.LogWarning("Text truncated from {Length} to {Max} chars", text.Length, MaxChars);
            text = text[..MaxChars];
        }

        var prompt = "Fasse den folgenden Dokumenttext in höchstens 5 Sätzen zusammen. " +
                     "Antworte in der Sprache des Dokuments.\n\n" + text;

        var body = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };

        using var req = new HttpRequestMessage(HttpMethod.Post, $"v1beta/models/{o.Model}:generateContent")
        {
            Content = JsonContent.Create(body)
        };
        req.Headers.Add("x-goog-api-key", o.ApiKey);

        HttpResponseMessage res;
        try
        {
            res = await http.SendAsync(req, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new GeminiException($"Gemini request failed: {ex.Message}", true);
        }

        using (res)
        {
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync(ct);
                var status = (int)res.StatusCode;
                var isDailyQuota = err.Contains("PerDay", StringComparison.OrdinalIgnoreCase);
                var transient = status >= 500 || (status == 429 && !isDailyQuota);
                throw new GeminiException($"Gemini returned {status}: {err}", transient);
            }

            using var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            try
            {
                var summary = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return string.IsNullOrWhiteSpace(summary)
                    ? throw new GeminiException("Gemini returned an empty summary", false)
                    : summary.Trim();
            }
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
            {
                throw new GeminiException("Unexpected Gemini response format (blocked or empty answer?)", false);
            }
        }
    }
}