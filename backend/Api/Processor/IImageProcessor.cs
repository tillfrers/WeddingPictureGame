namespace Api.Processor;

public interface IImageProcessor
{
    public Task UploadImage(IFormFile file, CancellationToken cancellationToken = default);
}