using Api.Dto;
using Api.Processor;
using Api.Repository;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controller;

[ApiController]
[Route("api/[controller]")]
[ProducesErrorResponseType(typeof(ProblemDetails))]
public sealed class ImageController(IImageProcessor imageProcessor, IImageRepository imageRepository, IFileProcessor fileProcessor) : ControllerBase
{
    private const long MaxFileSize = 31_457_280; // 30 MB
    private const string ImmutableCacheControl = "public, max-age=31536000, immutable";
    private const string JpegContentType = "image/jpeg";

    private static readonly HashSet<string> AllowedExtensions =
        [".jpg", ".jpeg", ".png"];

    [HttpPost("{table}/upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSize)]
    [ProducesResponseType<UploadResultDto>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge, "application/problem+json")]
    public async Task<IActionResult> Upload(string table, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return BadRequestProblem("Es wurde keine Datei ausgewählt");

        if (file.Length > MaxFileSize)
            return BadRequestProblem("Das Bild ist zu groß");

        if (!Constants.Constants.TableIds.TryGetValue(table, out var tableId))
            return BadRequestProblem("Dieser Tisch existiert nicht");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension))
            return BadRequestProblem($"Format '{extension}' nicht erlaubt.");

        var id = await imageProcessor.TransformAndSaveAsync(file, tableId, cancellationToken);

        return Created($"/api/Image/{table}/{id}/display", new UploadResultDto(id));
    }

    [HttpGet("{table}/gallery")]
    [ProducesResponseType<PagedResult<GalleryDto>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<PagedResult<GalleryDto>>> GetGallery(string table, [FromQuery] int page, CancellationToken cancellationToken)
    {
        if (page <= 0)
            return BadRequestProblem("Falscher Seiten Parameter");

        if (!Constants.Constants.TableIds.TryGetValue(table, out var id))
            return BadRequestProblem("Dieser Tisch existiert nicht");

        var result = await imageRepository.GetGalleryAsync(id, page, cancellationToken);

        return Ok(new PagedResult<GalleryDto>(result.Item1, page, Constants.Constants.PageSizeGallery, result.Item2, id));
    }

    [HttpGet("{table}/{id}/display")]
    [ProducesResponseType<FileResult>(StatusCodes.Status200OK, JpegContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public IActionResult GetDisplay(string table, string id)
    {
        if (!Constants.Constants.TableIds.TryGetValue(table, out var tableId))
            return BadRequestProblem("Dieser Tisch existiert nicht");

        if (!Guid.TryParse(id, out var guid))
            return BadRequestProblem("Die Id ist ungültig");

        string path;
        try
        {
            path = fileProcessor.GetDisplayPath(guid, tableId);
        }
        catch (FileNotFoundException)
        {
            return NotFoundProblem("Bild nicht gefunden");
        }

        Response.Headers.CacheControl = ImmutableCacheControl;
        return PhysicalFile(path, JpegContentType);
    }

    [HttpGet("{table}/{id}/thumbnail")]
    [ProducesResponseType<FileResult>(StatusCodes.Status200OK, JpegContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public IActionResult GetThumbnail(string table, string id)
    {
        if (!Constants.Constants.TableIds.TryGetValue(table, out var tableId))
            return BadRequestProblem("Dieser Tisch existiert nicht");

        if (!Guid.TryParse(id, out var guid))
            return BadRequestProblem("Die Id ist ungültig");

        string path;
        try
        {
            path = fileProcessor.GetThumbnailPath(guid, tableId);
        }
        catch (FileNotFoundException)
        {
            return NotFoundProblem("Thumbnail nicht gefunden");
        }

        Response.Headers.CacheControl = ImmutableCacheControl;
        return PhysicalFile(path, JpegContentType);
    }

    private ObjectResult BadRequestProblem(string detail) =>
        Problem(
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest,
            title: "Ungültige Anfrage");

    private ObjectResult NotFoundProblem(string detail) =>
        Problem(
            detail: detail,
            statusCode: StatusCodes.Status404NotFound,
            title: "Nicht gefunden");
}