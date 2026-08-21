using System.Reflection.Metadata;
using Api.Dto;
using Api.Processor;
using Api.Repository;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controller;

[ApiController]
[Route("api/[controller]")]
public sealed class ImageController(IImageProcessor imageProcessor, IImageRepository imageRepository, IFileProcessor fileProcessor) : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions =
        [".jpg", ".jpeg", ".png"];

    [HttpPost("{table}/upload")]
    public async Task<IActionResult> Upload(string table, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return BadRequest("Es wurde keine Datei ausgewählt");
        
        if (!Constants.Constants.TableIds.ContainsKey(table))
            return BadRequest("Dieser Tisch existiert nicht");
 
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!AllowedExtensions.Contains(extension))
            return BadRequest($"Format '{extension}' nicht erlaubt.");
        
        await imageProcessor.TransformAndSaveAsync(file, Constants.Constants.TableIds[table], cancellationToken);

        return Ok();
    }
    
    [HttpGet("{table}/gallery")]
    public async Task<ActionResult<PagedResult<GalleryDto>>> GetGallery(string table, [FromQuery] int page, CancellationToken cancellationToken)
    {
        if (page <= 0)
            return BadRequest("Falscher Seiten Parameter");
        
        if (!Constants.Constants.TableIds.TryGetValue(table, out var id))
            return BadRequest("Dieser Tisch existiert nicht");
 
        var result = await imageRepository.GetGalleryAsync(id, page, cancellationToken);
        
        return Ok(new PagedResult<GalleryDto>(result.Item1, page, Constants.Constants.PageSizeGallery, result.Item2));
    }
    
    [HttpGet("{table}/{id}/display")]
    public async Task<ActionResult> GetDisplay(string table, string id, CancellationToken cancellationToken)
    {
        if (!Constants.Constants.TableIds.TryGetValue(table, out var tableId))
            return BadRequest("Dieser Tisch existiert nicht");
 
        byte[] bytes;
        try
        {
            bytes = await fileProcessor.LoadDisplayAsync(Guid.Parse(id), tableId, cancellationToken);
        }
        catch (FileNotFoundException)
        {
            return NotFound("Bild nicht gefunden");
        }

        return File(bytes, "image/jpeg");
    }
    
    [HttpGet("{table}/{id}/thumbnail")]
    public async Task<ActionResult> GetThumbnail(string table, string id, CancellationToken cancellationToken)
    {
        if (!Constants.Constants.TableIds.TryGetValue(table, out var tableId))
            return BadRequest("Dieser Tisch existiert nicht");

        byte[] bytes;
        try
        {
            bytes = await fileProcessor.LoadThumbnailAsync(Guid.Parse(id), tableId, cancellationToken);
        }
        catch (FileNotFoundException)
        {
            return NotFound("Thumbnail nicht gefunden");
        }
        
        if (bytes.Length == 0)
            return NotFound();

        return File(bytes, "image/jpeg");
    }
}