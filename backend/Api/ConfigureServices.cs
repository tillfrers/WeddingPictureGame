using Api.Options;
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
        
        services.Configure<ImagePathOptions>(configuration);
        services.Configure<CapabilityHashOptions>(configuration);
        
        services.AddOptions<CapabilityHashOptions>()
            .Bind(configuration.GetSection(CapabilityHashOptions.SectionName))
            .Validate(o => o. TablesHash is { Length: 64 } && o.TablesHash.All(Uri.IsHexDigit),
                "TablesHash muss ein 64-stelliger SHA-256-Hex-String sein.")
            .Validate(o => o.AllHash is { Length: 64 } && o.AllHash.All(Uri.IsHexDigit),
                "AllHash muss ein 64-stelliger SHA-256-Hex-String sein.")
            .ValidateOnStart();
        
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
