using System.ComponentModel.DataAnnotations;

namespace Api.Entities;

public class Images
{
    public Guid Id { get; set; }
    
    [Range(1, 8)]
    public int Tables { get; set; }

    public byte[] Image { get; set; } = [];
    
    public byte[] Thumbnail { get; set; } = [];
    
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}