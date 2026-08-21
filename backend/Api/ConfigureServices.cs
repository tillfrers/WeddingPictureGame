using Api.Persistence;
using Api.Processor;
using Api.Repository;
using Microsoft.EntityFrameworkCore;

namespace Api;

public static class ConfigureServices
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        
        services.AddScoped<IImageProcessor, ImageProcessor>();
        services.AddScoped<IImageRepository, ImageRepository>();
        services.AddScoped<IFileProcessor, FileProcessor>();
        
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
