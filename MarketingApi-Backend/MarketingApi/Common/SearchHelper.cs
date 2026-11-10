namespace MarketingApi.Common;

public static class SearchHelper
{
    /// <summary>
    /// Escapes PostgreSQL LIKE/ILIKE wildcard characters (%, _, \) so user input is matched literally.
    /// </summary>
    public static string EscapeLikePattern(string input)
    {
        return input
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
    }
}
