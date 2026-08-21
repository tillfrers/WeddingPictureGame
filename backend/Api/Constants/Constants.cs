namespace Api.Constants;

public static class Constants
{
    public const int PageSizeGallery = 10;
    
    public static readonly Dictionary<int, string> TableNames = new()
    {
        {1, "9E79"},
        {2, "859D"},
        {3, "9B42"},
        {4, "81E6"},
        {5, "831D"},
        {6, "B059"},
        {7, "A816"},
        {8, "A93A"}
    };
    
    public static readonly Dictionary<string, int> TableIds =
        TableNames.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.OrdinalIgnoreCase);
}