using Api.Dto;
using Api.Processor;
using Api.Repository;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controller;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class ImageController(IImageProcessor imageProcessor, IImageRepository imageRepository, IFileProcessor fileProcessor) : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions =
        [".jpg", ".jpeg", ".png"];

    [HttpPost("{table}/upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(31_457_280)]
    [ProducesResponseType<UploadResultDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(string table, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return BadRequest("Es wurde keine Datei ausgewählt");
        
        if (file.Length > 31457280) //30mb
            return BadRequest("Das Bild ist zu groß");
        
        if (!Constants.Constants.TableIds.ContainsKey(table))
            return BadRequest("Dieser Tisch existiert nicht");
 
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!AllowedExtensions.Contains(extension))
            return BadRequest($"Format '{extension}' nicht erlaubt.");
        
        var id = await imageProcessor.TransformAndSaveAsync(file, Constants.Constants.TableIds[table], cancellationToken);

        return Created($"/api/Image/{table}/{id}/display", new UploadResultDto(id));
    }
    
    [HttpGet("{table}/gallery")]
    [ProducesResponseType<PagedResult<GalleryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<GalleryDto>>> GetGallery(string table, [FromQuery] int page, CancellationToken cancellationToken)
    {
        if (page <= 0)
            return BadRequest("Falscher Seiten Parameter");
        
        if (!Constants.Constants.TableIds.TryGetValue(table, out var id))
            return BadRequest("Dieser Tisch existiert nicht");
 
        var result = await imageRepository.GetGalleryAsync(id, page, cancellationToken);
        
        return Ok(new PagedResult<GalleryDto>(result.Item1, page, Constants.Constants.PageSizeGallery, result.Item2));
    }
    
    private const string ImmutableCacheControl = "public, max-age=31536000, immutable";

    [HttpGet("{table}/{id}/display")]
    [Produces("image/jpeg")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult GetDisplay(string table, string id)
    {
        if (!Constants.Constants.TableIds.TryGetValue(table, out var tableId))
            return BadRequest("Dieser Tisch existiert nicht");

        if (!Guid.TryParse(id, out var guid))
            return BadRequest("Die Id ist ungültig");

        string path;
        try
        {
            path = fileProcessor.GetDisplayPath(guid, tableId);
        }
        catch (FileNotFoundException)
        {
            return NotFound("Bild nicht gefunden");
        }

        Response.Headers["Cache-Control"] = ImmutableCacheControl;
        return PhysicalFile(path, "image/jpeg");
    }

    [HttpGet("{table}/{id}/thumbnail")]
    [Produces("image/jpeg")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult GetThumbnail(string table, string id)
    {
        if (!Constants.Constants.TableIds.TryGetValue(table, out var tableId))
            return BadRequest("Dieser Tisch existiert nicht");

        if (!Guid.TryParse(id, out var guid))
            return BadRequest("Die Id ist ungültig");

        string path;
        try
        {
            path = fileProcessor.GetThumbnailPath(guid, tableId);
        }
        catch (FileNotFoundException)
        {
            return NotFound("Thumbnail nicht gefunden");
        }

        Response.Headers["Cache-Control"] = ImmutableCacheControl;
        return PhysicalFile(path, "image/jpeg");
    }
}