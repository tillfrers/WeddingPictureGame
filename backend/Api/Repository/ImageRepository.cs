using Api.Dto;
using Api.Entities;
using Api.Persistence;
using Api.Processor;
using Microsoft.EntityFrameworkCore;

namespace Api.Repository;

public class ImageRepository(AppDbContext dbContext, IFileProcessor fileProcessor, TimeProvider provider) : IImageRepository
{
    public async Task SaveImageAsync(Guid id, int table, string original, string display, string thumbnail, CancellationToken cancellationToken = default)
    {
        var images = new Images
        {
            Id = id,
            Tables = table,
            PathOriginal = original,
            PathDisplay = display,
            PathThumbnail = thumbnail,
            DateCreated = provider.GetUtcNow().DateTime.ToUniversalTime()
        };
        
        dbContext.Images.Add(images);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Images> GetImage(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Images.SingleAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<GalleryDto>, int)> GetGalleryAsync(int table, int page, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Images.AsNoTracking()
            .Where(i =>  i.Tables == table)
            .OrderBy(i => i.DateCreated);

        var total = await query.CountAsync(cancellationToken: cancellationToken);

        var images = await query
            .Skip((page - 1) * Constants.Constants.PageSizeGallery)
            .Take(Constants.Constants.PageSizeGallery)
            .ToListAsync(cancellationToken: cancellationToken);

        return(
        [
            .. images.Select(i => new GalleryDto
            {
                Id = i.Id,
                ThumbnailUrl = $"/api/images/{Constants.Constants.TableNames[table]}/{i.Id}/thumbnail"
            })
        ], total);
    }
}