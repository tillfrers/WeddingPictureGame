using Api;
using Api.Persistence;
using Api.Processor;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApiDocument(options =>
{
    options.Title = "WeddingPictureGame API";
    options.Version = "v1";
});
builder.Services.AddServices(builder.Configuration);

builder.Services.Configure<FormOptions>(o => o.MemoryBufferThreshold = 24 * 1024 * 1024);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();      
    app.UseSwaggerUi();    
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    
    var fileProcessor = scope.ServiceProvider.GetRequiredService<IFileProcessor>();
    fileProcessor.EnsurePathExists();
}

app.UseHttpsRedirection();
app.MapControllers();

app.UseExceptionHandler(); 
app.UseStatusCodePages();

app.Run();