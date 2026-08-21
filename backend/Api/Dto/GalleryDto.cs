namespace Api.Dto;

public sealed record GalleryDto
{
    public Guid Id { get; init; }

    public required string ThumbnailUrl { get; init; }
}