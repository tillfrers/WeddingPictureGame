using Api.Persistence;
using Api.Processor;
using Microsoft.EntityFrameworkCore;

namespace Api;

public static class ConfigureServices
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        
        services.AddScoped<IImageProcessor, ImageProcessor>();

        return services;
    }
}
