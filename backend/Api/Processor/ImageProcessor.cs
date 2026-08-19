using Api.Repository;
using SkiaSharp;

namespace Api.Processor;

public class ImageProcessor(IImageRepository imageRepository) : IImageProcessor
{
    public async Task TransformAndSaveAsync(IFormFile file, int table, CancellationToken cancellationToken = default)
    {
        using var imageStream = new MemoryStream(); 
        await file.CopyToAsync(imageStream, cancellationToken);
        imageStream.Position = 0;
        
        var thumbnail = CreateThumbnailAsync(imageStream.ToArray());
        
        await imageRepository.SaveImageAsync(table, imageStream.ToArray(), thumbnail.ToArray(), cancellationToken: cancellationToken);
    }

    private static byte[] CreateThumbnailAsync(byte[] source, int size = 300, int quality = 80)
    {
        using var data = SKData.CreateCopy(source);
        using var codec = SKCodec.Create(data)
            ?? throw new InvalidOperationException("Bildformat nicht lesbar.");

        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height,
                                   SKColorType.Bgra8888, SKAlphaType.Premul);

        using var decoded  = SKBitmap.Decode(codec, info);
        using var rotated  = ApplyOrientation(decoded, codec.EncodedOrigin);
        var bitmap = rotated ?? decoded;
        
        var edge = Math.Min(bitmap.Width, bitmap.Height);
        var src  = SKRectI.Create((bitmap.Width - edge) / 2,
                                  (bitmap.Height - edge) / 2, edge, edge);

        using var surface = SKSurface.Create(
            new SKImageInfo(size, size, SKColorType.Bgra8888, SKAlphaType.Premul));

        using var image = SKImage.FromBitmap(bitmap);
        surface.Canvas.DrawImage(image, src, SKRect.Create(size, size),
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        surface.Canvas.Flush();

        using var snapshot = surface.Snapshot();
        using var encoded  = snapshot.Encode(SKEncodedImageFormat.Webp, quality);
        return encoded.ToArray();
    }

    private static SKBitmap? ApplyOrientation(SKBitmap bitmap, SKEncodedOrigin origin)
    {
        if (origin is SKEncodedOrigin.Default)
            return null;

        var swap = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;

        var w = swap ? bitmap.Height : bitmap.Width;
        var h = swap ? bitmap.Width  : bitmap.Height;

        var rotated = new SKBitmap(w, h, bitmap.ColorType, bitmap.AlphaType);
        using var canvas = new SKCanvas(rotated);

        switch (origin)
        {
            case SKEncodedOrigin.TopRight:     canvas.Scale(-1, 1, bitmap.Width / 2f, 0); break;
            case SKEncodedOrigin.BottomRight:  canvas.RotateDegrees(180, w / 2f, h / 2f); break;
            case SKEncodedOrigin.BottomLeft:   canvas.Scale(1, -1, 0, bitmap.Height / 2f); break;
            case SKEncodedOrigin.LeftTop:      canvas.RotateDegrees(90, 0, 0);
                canvas.Scale(1, -1, 0, 0); break;
            case SKEncodedOrigin.RightTop:     canvas.Translate(w, 0);
                canvas.RotateDegrees(90); break;
            case SKEncodedOrigin.RightBottom:  canvas.Translate(w, 0);
                canvas.RotateDegrees(90);
                canvas.Scale(1, -1, 0, bitmap.Height / 2f); break;
            case SKEncodedOrigin.LeftBottom:   canvas.Translate(0, h);
                canvas.RotateDegrees(270); break;
            default:
                throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
        }

        using var src = SKImage.FromBitmap(bitmap);
        canvas.DrawImage(src, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest));
        canvas.Flush();

        return rotated;
    }
}


