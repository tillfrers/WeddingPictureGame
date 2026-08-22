using Api.Options;
using Microsoft.Extensions.Options;

namespace Api.Processor;

public class FileProcessor(IOptions<ImagePathOptions> options) : IFileProcessor
{
    private string ImageRootPath => 
        options.Value.ImagePath ?? Path.Combine(Environment.CurrentDirectory,"images");
    
    private string GetTableDirectory(int i) => Path.Combine(ImageRootPath, i.ToString());
    private string GetOriginalDirectory(int i) => Path.Combine(GetTableDirectory(i), "original");
    private string GetDisplayDirectory(int i) => Path.Combine(GetTableDirectory(i), "display");
    private string GetThumbnailDirectory(int i) => Path.Combine(GetTableDirectory(i), "thumbnail");
    
    public void EnsurePathExists()
    { 
        for (var i = 1; i <= 8; i++)
        { 
            if (!Directory.Exists(GetOriginalDirectory(i)))
                Directory.CreateDirectory(GetOriginalDirectory(i)); 
            
            if (!Directory.Exists(GetDisplayDirectory(i)))
                Directory.CreateDirectory(GetDisplayDirectory(i)); 
            
            if (!Directory.Exists(GetThumbnailDirectory(i)))
                Directory.CreateDirectory(GetThumbnailDirectory(i)); 
        }
    }

    public async Task<Tuple<string, string, string>> SaveImageAsync(Guid id, int table, byte[] original, byte[] display, byte[] thumbnail, CancellationToken cancellationToken)
    {
        var pathOriginal = Path.Combine(GetOriginalDirectory(table), id + ".jpeg");
        var pathDisplay = Path.Combine(GetDisplayDirectory(table), id + ".jpeg");
        var pathThumbnail = Path.Combine(GetThumbnailDirectory(table), id + ".jpeg");
        
        await File.WriteAllBytesAsync(pathOriginal, original, cancellationToken);
        await File.WriteAllBytesAsync(pathDisplay, display, cancellationToken);
        await File.WriteAllBytesAsync(pathThumbnail, thumbnail, cancellationToken);

        return new Tuple<string, string, string>(pathOriginal, pathDisplay, pathThumbnail);
    }

    public string GetDisplayPath(Guid id, int table)
    {
        var path = Path.Combine(GetDisplayDirectory(table), id + ".jpeg");

        if (!File.Exists(path))
            throw new FileNotFoundException();

        return path;
    }

    public string GetThumbnailPath(Guid id, int table)
    {
        var path = Path.Combine(GetThumbnailDirectory(table), id + ".jpeg");

        if (!File.Exists(path))
            throw new FileNotFoundException();

        return path;
    }
}
