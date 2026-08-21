using Api.Dto;
using Api.Entities;

namespace Api.Repository;

public interface IImageRepository
{
    Task SaveImageAsync(Guid id, int table, string original, string display, string thumbnail, CancellationToken cancellationToken = default);
    
    Task<Images> GetImage(Guid id, CancellationToken cancellationToken = default);
    
    Task<(IReadOnlyList<GalleryDto>, int)> GetGalleryAsync(int table, int page, CancellationToken cancellationToken = default);
}