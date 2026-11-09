using System.Net;
using DocumentManagementSystem.Services;

namespace DocumentManagementSystem.Test.Presentation;

public class ElasticsearchDocumentSearchServiceTest
{
    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task DeleteAsync_AcceptsDeletedOrMissingEntry(HttpStatusCode status)
    {
        var handler = new TestHttpHandler(status);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:9200/") };
        var service = new ElasticsearchDocumentSearchService(client);

        await service.DeleteAsync(7, CancellationToken.None);

        Assert.Equal(HttpMethod.Delete, handler.Method);
        Assert.Equal("/documents/_doc/7?refresh=wait_for", handler.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task DeleteAsync_ReportsElasticsearchFailure()
    {
        var handler = new TestHttpHandler(HttpStatusCode.ServiceUnavailable);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:9200/") };
        var service = new ElasticsearchDocumentSearchService(client);

        await Assert.ThrowsAsync<HttpRequestException>(() => service.DeleteAsync(7, CancellationToken.None));
    }

    // Kleine HTTP-Antwort ohne echten Elasticsearch-Server.
    private class TestHttpHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        public HttpMethod? Method { get; private set; }
        public Uri? RequestUri { get; private set; }

        public TestHttpHandler(HttpStatusCode status)
        {
            _status = status;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(_status));
        }
    }
}
