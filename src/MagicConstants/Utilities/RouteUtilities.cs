using MagicConstants.Core;

namespace MagicConstants.Utilities;

/// <summary>
/// Provides route-related utility functions.
/// </summary>
internal static class RouteUtilities
{
    /// <summary>
    /// Normalizes route names by handling index files and extension removal.
    /// </summary>
    public static string NormalizeRouteName(string filename, string extension, bool removeRouteExtension)
    {
        string routeName = filename.Replace("\\", "/");
        
        if (routeName is Constants.IndexHtml or Constants.IndexHtm)
        {
            return string.Empty;
        }
        
        if (routeName.EndsWith(Constants.IndexHtmPath))
        {
            return routeName.Substring(0, routeName.Length - Constants.IndexHtmPath.Length);
        }
        
        if (routeName.EndsWith(Constants.IndexHtmlPath))
        {
            return routeName.Substring(0, routeName.Length - Constants.IndexHtmlPath.Length);
        }
        
        if (removeRouteExtension)
        {
            return routeName.Substring(0, routeName.Length - extension.Length);
        }
        
        return routeName;
    }

    /// <summary>
    /// Gets the appropriate MIME type for a file extension.
    /// </summary>
    public static string GetMimeType(string extension)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" => "MediaTypeNames.Image.Jpeg",
            ".gif" => "MediaTypeNames.Image.Gif",
            ".bmp" => "MediaTypeNames.Image.Bmp",
            ".ico" => "MediaTypeNames.Image.Icon",
            ".png" => "MediaTypeNames.Image.Png",
            ".svg" => "MediaTypeNames.Image.Svg",
            ".webp" => "MediaTypeNames.Image.Webp",
            ".html" or ".htm" => "MediaTypeNames.Text.Html",
            ".css" => "MediaTypeNames.Text.Css",
            ".js" => "MediaTypeNames.Text.JavaScript",
            ".xml" => "MediaTypeNames.Text.Xml",
            _ => "MediaTypeNames.Text.Plain",
        };
    }
}
