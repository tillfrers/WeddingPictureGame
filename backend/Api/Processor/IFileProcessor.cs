namespace Api.Processor;

public interface IFileProcessor
{
    void EnsurePathExists();
    
    Task<Tuple<string, string, string>> SaveImageAsync(Guid id, int table, byte[] original, byte[] display, byte[] thumbnail, CancellationToken cancellationToken);

    Task<byte[]> LoadDisplayAsync(Guid id, int table, CancellationToken cancellationToken);

    Task<byte[]> LoadThumbnailAsync(Guid id, int table, CancellationToken cancellationToken);
}