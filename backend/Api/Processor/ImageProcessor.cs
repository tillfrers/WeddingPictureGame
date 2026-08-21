using Api.Repository;
using SkiaSharp;

namespace Api.Processor;

public class ImageProcessor(IImageRepository imageRepository, IFileProcessor fileProcessor) : IImageProcessor
{
    public async Task TransformAndSaveAsync(IFormFile file, int table, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        using var imageStream = new MemoryStream(); 
        await file.CopyToAsync(imageStream, cancellationToken);
        imageStream.Position = 0;
        
        var bytes = imageStream.ToArray(); 
        var displayTask   = Task.Run(() => RescaleImage(bytes, 1080, false), cancellationToken);
        var thumbnailTask = Task.Run(() => RescaleImage(bytes, 400,  true),  cancellationToken);

        await Task.WhenAll(displayTask, thumbnailTask);
        
        var result = await fileProcessor.SaveImageAsync(id, table, imageStream.ToArray(), displayTask.Result, thumbnailTask.Result, cancellationToken);
        
        await imageRepository.SaveImageAsync(id, table, result.Item1, result.Item2, result.Item3, cancellationToken: cancellationToken);
    }

    private static byte[] RescaleImage(byte[] source, int minSize, bool square)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minSize);

        using var data = SKData.CreateCopy(source);
        using var codec = SKCodec.Create(data)
            ?? throw new InvalidOperationException("Bildformat nicht lesbar.");

        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height,
                                   SKColorType.Bgra8888, SKAlphaType.Premul);

        using var decoded = SKBitmap.Decode(codec, info);
        using var rotated = ApplyOrientation(decoded, codec.EncodedOrigin);
        var bitmap = rotated ?? decoded;

        var shortEdge = Math.Min(bitmap.Width, bitmap.Height);
        var target = Math.Min(minSize, shortEdge);          // kein Upscaling

        SKRectI src;
        SKImageInfo dstInfo;

        if (square)
        {
            src = SKRectI.Create((bitmap.Width  - shortEdge) / 2,
                                 (bitmap.Height - shortEdge) / 2,
                                 shortEdge, shortEdge);
            dstInfo = new SKImageInfo(target, target,
                                      SKColorType.Bgra8888, SKAlphaType.Premul);
        }
        else
        {
            src = SKRectI.Create(0, 0, bitmap.Width, bitmap.Height);

            var scale = (double)target / shortEdge;
            var w = Math.Max(1, (int)Math.Round(bitmap.Width  * scale));
            var h = Math.Max(1, (int)Math.Round(bitmap.Height * scale));

            dstInfo = new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Premul);
        }

        using var surface = SKSurface.Create(dstInfo);
        surface.Canvas.Clear(SKColors.White);               // JPEG kennt kein Alpha

        using var image = SKImage.FromBitmap(bitmap);
        surface.Canvas.DrawImage(image, src,
            SKRect.Create(dstInfo.Width, dstInfo.Height),
            new SKSamplingOptions(SKCubicResampler.Mitchell));
        surface.Canvas.Flush();

        using var snapshot = surface.Snapshot();
        using var encoded  = snapshot.Encode(SKEncodedImageFormat.Jpeg, 80);
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


