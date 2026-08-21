namespace Api.Processor;

public interface IImageProcessor
{
    public Task<Guid> TransformAndSaveAsync(IFormFile file, int table, CancellationToken cancellationToken = default);
}