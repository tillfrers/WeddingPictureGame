namespace Api.Options;

public class CapabilityHashOptions
{
    public const string SectionName = "CapabilityHash";
    
    public string TablesHash { get; set; } = string.Empty;
    public string AllHash { get; set; } = string.Empty;

}