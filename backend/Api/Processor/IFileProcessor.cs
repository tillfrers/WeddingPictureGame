namespace Api.Processor;

public interface IFileProcessor
{
    void EnsurePathExists();
    
    Task<Tuple<string, string, string>> SaveImageAsync(Guid id, int table, byte[] original, byte[] display, byte[] thumbnail, CancellationToken cancellationToken);

    string GetDisplayPath(Guid id, int table);

    string GetThumbnailPath(Guid id, int table);
    
    string GetOriginalPath(Guid id);
}