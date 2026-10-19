using DocumentManagementSystem.OcrWorker.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<OcrWorkerService>();

var host = builder.Build();
host.Run();