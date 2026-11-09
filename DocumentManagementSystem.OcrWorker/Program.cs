using DocumentManagementSystem.OcrWorker.Services;
using DocumentManagementSystem.BusinessLogic.Storage;
using DocumentManagementSystem.Storage.Interfaces;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<IFileStorage, MinioFileStorage>();
builder.Services.AddHostedService<OcrWorkerService>();

var host = builder.Build();
host.Run();
