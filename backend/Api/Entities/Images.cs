using System.ComponentModel.DataAnnotations;

namespace Api.Entities;

public class Images
{
    public Guid Id { get; set; }
    
    [Range(1, 8)]
    public int Tables { get; set; }

    public required string PathOriginal { get; set; }
    
    public required string PathDisplay { get; set; }
    
    public required string PathThumbnail { get; set; }
    
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}