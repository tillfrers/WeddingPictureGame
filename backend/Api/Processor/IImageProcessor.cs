namespace Api.Processor;

public interface IImageProcessor
{
    public Task TransformAndSaveAsync(IFormFile file, int table, CancellationToken cancellationToken = default);
}