namespace Api.Dto;

public sealed record GalleryDto
{
    public Guid Id { get; init; }

    public byte[] Thumbnail { get; init; } = [];
}