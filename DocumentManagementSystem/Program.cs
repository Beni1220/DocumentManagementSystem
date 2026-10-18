using DocumentManagementSystem.BusinessLogic.Messaging;
using DocumentManagementSystem.BusinessLogic.Messaging.Interfaces;
using DocumentManagementSystem.BusinessLogic.Services;          
using DocumentManagementSystem.BusinessLogic.Services.Interfaces;
using DocumentManagementSystem.DataAccess.Context;
using DocumentManagementSystem.DataAccess.Repositories;
using DocumentManagementSystem.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DMSContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default"),
        b => b.MigrationsAssembly("DocumentManagementSystem.DataAccess")));

builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IMessagePublisher, RabbitMqPublisher>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(Program).Assembly));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DMSContext>();
    db.Database.Migrate();
}

/*
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Document Management System API V1");
    });
}
*/

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();