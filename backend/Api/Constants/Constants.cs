namespace Api.Constants;

public static class Constants
{
    public const int PageSizeGallery = 12;
    
    public static readonly Dictionary<int, string> TableNames = new()
    {
        {0, "43A2"},
        {1, "4D60"},
        {2, "4EBA"},
        {3, "4E95"},
        {4, "99BD"},
        {5, "2DAE"},
        {6, "4DE4"},
        {7, "BA1B"},
        {8, "A00D"}
    };
    
    public static readonly Dictionary<string, int> TableIds =
        TableNames.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.OrdinalIgnoreCase);
}