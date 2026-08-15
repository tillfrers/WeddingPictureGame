namespace Api.Processor;

public class ImageProcessor : IImageProcessor
{
    public Task UploadImage(IFormFile file, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}