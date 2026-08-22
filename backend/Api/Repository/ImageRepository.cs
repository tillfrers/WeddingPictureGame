using Api.Dto;
using Api.Entities;
using Api.Persistence;
using Api.Processor;
using Microsoft.EntityFrameworkCore;

namespace Api.Repository;

public class ImageRepository(AppDbContext dbContext, TimeProvider provider) : IImageRepository
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

    public Task<(IReadOnlyList<GalleryDto>, int)> GetGalleryAsync(int table, int page, CancellationToken cancellationToken = default) =>
        PageAsync(dbContext.Images.AsNoTracking().Where(i => i.Tables == table), page, cancellationToken);

    public Task<(IReadOnlyList<GalleryDto>, int)> GetGalleryAllAsync(int page, CancellationToken cancellationToken = default) =>
        PageAsync(dbContext.Images.AsNoTracking(), page, cancellationToken);
    
    private static async Task<(IReadOnlyList<GalleryDto>, int)> PageAsync(IQueryable<Images> query, int page, CancellationToken cancellationToken)
    {
        var ordered = query.OrderBy(i => i.DateCreated).ThenBy(i => i.Id);

        var total = await ordered.CountAsync(cancellationToken);

        var images = await ordered
            .Skip((page - 1) * Constants.Constants.PageSizeGallery)
            .Take(Constants.Constants.PageSizeGallery)
            .ToListAsync(cancellationToken);

        return (
        [
            .. images.Select(i => new GalleryDto
            {
                Id = i.Id,
                ThumbnailUrl = $"/api/Image/{Constants.Constants.TableNames[i.Tables]}/{i.Id}/thumbnail"
            })
        ], total);
    }
}