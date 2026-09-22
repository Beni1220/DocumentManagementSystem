using Microsoft.EntityFrameworkCore;
using DocumentManagementSystem.DataAccess.Context;
using DocumentManagementSystem.BusinessLogic.Services.Interfaces;
using DocumentManagementSystem.BusinessLogic.Services;          
using DocumentManagementSystem.DataAccess.Repositories.Interfaces;
using DocumentManagementSystem.DataAccess.Repositories;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DMSContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default"),
        b => b.MigrationsAssembly("DocumentManagementSystem.DataAccess")));

builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>(); 

builder.Services.AddControllers();
builder.Services.AddOpenApi();

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