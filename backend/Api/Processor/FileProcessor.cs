using System.Runtime.InteropServices;

namespace Api.Processor;

public class FileProcessor : IFileProcessor
{
    private string ImageRootPath => Path.Combine(Environment.CurrentDirectory,"images");
    
    private string GetTablePath(int i) => Path.Combine(ImageRootPath, i.ToString());
    private string GetOriginalPath(int i) => Path.Combine(GetTablePath(i), "original");
    private string GetDisplayPath(int i) => Path.Combine(GetTablePath(i), "display");
    private string GetThumbnailPath(int i) => Path.Combine(GetTablePath(i), "thumbnail");
    
    public void EnsurePathExists()
    { 
        for (var i = 1; i <= 8; i++)
        { 
            if (!Directory.Exists(GetOriginalPath(i)))
                Directory.CreateDirectory(GetOriginalPath(i)); 
            
            if (!Directory.Exists(GetDisplayPath(i)))
                Directory.CreateDirectory(GetDisplayPath(i)); 
            
            if (!Directory.Exists(GetThumbnailPath(i)))
                Directory.CreateDirectory(GetThumbnailPath(i)); 
        }
    }

    public async Task<Tuple<string, string, string>> SaveImageAsync(Guid id, int table, byte[] original, byte[] display, byte[] thumbnail, CancellationToken cancellationToken)
    {
        var pathOriginal = Path.Combine(GetOriginalPath(table), id + ".jpeg");
        var pathDisplay = Path.Combine(GetDisplayPath(table), id + ".jpeg");
        var pathThumbnail = Path.Combine(GetThumbnailPath(table), id + ".jpeg");
        
        await File.WriteAllBytesAsync(pathOriginal, original, cancellationToken);
        await File.WriteAllBytesAsync(pathDisplay, display, cancellationToken);
        await File.WriteAllBytesAsync(pathThumbnail, thumbnail, cancellationToken);
        
        return new Tuple<string, string, string>(pathOriginal, pathDisplay, pathThumbnail);
    }
    
    public async Task<byte[]> LoadDisplayAsync(Guid id, int table, CancellationToken cancellationToken)
    {
        var pathThumbnail = Path.Combine(GetDisplayPath(table), id + ".jpeg");
        
        return await File.ReadAllBytesAsync(pathThumbnail, cancellationToken);
    }

    public async Task<byte[]> LoadThumbnailAsync(Guid id, int table, CancellationToken cancellationToken)
    {
        var pathThumbnail = Path.Combine(GetThumbnailPath(table), id + ".jpeg");
        
        return await File.ReadAllBytesAsync(pathThumbnail, cancellationToken);
    }
}