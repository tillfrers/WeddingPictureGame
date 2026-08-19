using Api.Dto;
using Api.Processor;
using Api.Repository;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controller;

[ApiController]
[Route("api/[controller]")]
public sealed class ImageController(IImageProcessor imageProcessor, IImageRepository imageRepository) : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions =
        [".jpg", ".jpeg", ".png"];

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] int table, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return BadRequest("Es wurde keine Datei ausgewählt");
 
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!AllowedExtensions.Contains(extension))
            return BadRequest($"Format '{extension}' nicht erlaubt.");
        
        await imageProcessor.TransformAndSaveAsync(file, table, cancellationToken);

        return Ok();
    }
    
    [HttpGet("gallery")]
    public async Task<ActionResult<PagedResult<GalleryDto>>> GetGallery([FromQuery] int table, int page, CancellationToken cancellationToken)
    {
        if (page <= 0)
            return BadRequest("Falscher Seiten Parameter");
        
        if (table is < 1 or > 8)
            return BadRequest("Falscher Tisch Parameter");
 
        var result = await imageRepository.GetGalleryAsync(table, page, cancellationToken);
        
        return Ok(new PagedResult<GalleryDto>(result.Item1, page, Constants.Constants.PageSizeGallery, result.Item2));
    }
}