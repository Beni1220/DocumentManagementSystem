using DocumentManagementSystem.IndexingWorker.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHttpClient<ElasticsearchIndexer>((services, client) =>
{
    var endpoint = services.GetRequiredService<IConfiguration>()["Elasticsearch:Url"]!;
    client.BaseAddress = new Uri(endpoint.EndsWith('/') ? endpoint : endpoint + "/");
});
builder.Services.AddHostedService<IndexingWorkerService>();

var host = builder.Build();
host.Run();
