using GenAIWorker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));
builder.Services.AddHttpClient<GeminiClient>(c =>
{
    c.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    c.Timeout = TimeSpan.FromSeconds(120);
});
builder.Services.AddHostedService<SummaryConsumer>();

await builder.Build().RunAsync();