using Api.Processor;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controller;

[ApiController]
[Route("api/[controller]")]
public sealed class ImageController(IImageProcessor imageProcessor) : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions =
        [".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".tiff"];

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return BadRequest("Es wurde keine Datei ausgewählt");
 
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!AllowedExtensions.Contains(extension))
            return BadRequest($"Format '{extension}' nicht erlaubt.");
 
        await imageProcessor.UploadImage(file, cancellationToken);

        return Ok();
    }
}