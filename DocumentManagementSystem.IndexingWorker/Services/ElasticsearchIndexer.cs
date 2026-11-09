using System.Net.Http.Json;
using Domain.Massages;
using Domain.Model;

namespace DocumentManagementSystem.IndexingWorker.Services;

public class ElasticsearchIndexer
{
    private readonly HttpClient _httpClient;

    public ElasticsearchIndexer(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task IndexAsync(OcrCompletedMessage message, CancellationToken cancellationToken)
    {
        var document = new SearchDocument
        {
            DocumentId = message.DocumentId,
            FileName = message.FileName,
            Description = message.Description,
            TextContent = message.TextContent
        };
        using var response = await _httpClient.PutAsJsonAsync(
            $"documents/_doc/{message.DocumentId}?refresh=wait_for", document, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
