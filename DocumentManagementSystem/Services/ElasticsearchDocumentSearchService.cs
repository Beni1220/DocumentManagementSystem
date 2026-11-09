using System.Net.Http.Json;
using System.Text.Json;
using Domain.Model;

namespace DocumentManagementSystem.Services;

public class ElasticsearchDocumentSearchService : IDocumentSearchService
{
    private const int MinLengthForFuzzy = 5;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public ElasticsearchDocumentSearchService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task DeleteAsync(int documentId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.DeleteAsync(
            $"documents/_doc/{documentId}?refresh=wait_for", cancellationToken);

        // Ein Dokument ohne fertige OCR hat noch keinen Indexeintrag.
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<SearchDocument>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        query = query.Trim();
        if (string.IsNullOrEmpty(query))
        {
            return Array.Empty<SearchDocument>();
        }

        var escapedQuery = query
            .Replace("\\", "\\\\")
            .Replace("*", "\\*")
            .Replace("?", "\\?");
        var wildcard = $"*{escapedQuery}*";

        var exactResults = await SendSearchRequestAsync(new
        {
            query = new
            {
                @bool = new
                {
                    should = new object[]
                    {
                        new { wildcard = new { fileName = new { value = wildcard, case_insensitive = true } } },
                        new { wildcard = new { description = new { value = wildcard, case_insensitive = true } } },
                        new { match_phrase = new { textContent = new { query } } }
                    },
                    minimum_should_match = 1
                }
            },
            size = 100
        }, cancellationToken);

        // Treffer vorhanden oder Suchbegriff zu kurz für sinnvolle Fuzzy-Suche
        if (exactResults.Count > 0 || query.Length < MinLengthForFuzzy)
        {
            return exactResults;
        }

        return await SendSearchRequestAsync(new
        {
            query = new
            {
                multi_match = new
                {
                    query,
                    fields = new[] { "fileName^3", "description^2", "textContent" },
                    fuzziness = "AUTO:5,8", // 0-4 Zeichen exakt, 5-7 = 1 Fehler, ab 8 = 2 Fehler
                    prefix_length = 2,
                    @operator = "and"
                }
            },
            size = 100
        }, cancellationToken);
    }

    private async Task<IReadOnlyList<SearchDocument>> SendSearchRequestAsync(object searchQuery,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync("documents/_search", searchQuery, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Array.Empty<SearchDocument>();
        }
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var hits = json.RootElement.GetProperty("hits").GetProperty("hits");
        var results = new List<SearchDocument>();
        foreach (var hit in hits.EnumerateArray())
        {
            var document = hit.GetProperty("_source").Deserialize<SearchDocument>(JsonOptions);
            if (document is not null)
            {
                results.Add(document);
            }
        }

        return results;
    }
}
