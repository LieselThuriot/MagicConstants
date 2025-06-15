namespace MagicConstants.Core;

/// <summary>
/// Provides file extension categorization and handling utilities.
/// </summary>
internal static class FileExtensions
{
    private static readonly HashSet<string> s_binaryExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".webp"
    };
    
    private static readonly HashSet<string> s_textProcessableExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".html", ".htm", ".css", ".js"
    };
    
    private static readonly HashSet<string> s_minifiableExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".html", ".htm", ".css", ".js"
    };

    /// <summary>
    /// Determines if a file extension represents a binary file.
    /// </summary>
    public static bool IsBinary(string extension) => s_binaryExtensions.Contains(extension);

    /// <summary>
    /// Determines if a file extension can be processed for templates.
    /// </summary>
    public static bool IsTextProcessable(string extension) => s_textProcessableExtensions.Contains(extension);

    /// <summary>
    /// Determines if a file extension can be minified.
    /// </summary>
    public static bool IsMinifiable(string extension) => s_minifiableExtensions.Contains(extension);

    /// <summary>
    /// Gets the priority order for file extensions in route generation.
    /// </summary>
    public static int GetPriority(string extension)
    {
        return extension switch
        {
            ".html" or ".htm" => Constants.HtmlExtensionPriority,
            ".css" => Constants.CssExtensionPriority,
            ".js" => Constants.JsExtensionPriority,
            _ => Constants.DefaultExtensionPriority
        };
    }
}
