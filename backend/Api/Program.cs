using Api;
using Api.Persistence;
using Api.Processor;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddServices(builder.Configuration);

builder.Services.Configure<FormOptions>(o => o.MemoryBufferThreshold = 24 * 1024 * 1024);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    
    var fileProcessor = scope.ServiceProvider.GetRequiredService<IFileProcessor>();
    fileProcessor.EnsurePathExists();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();