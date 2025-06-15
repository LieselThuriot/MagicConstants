namespace MagicConstants.Core;

/// <summary>
/// Contains all constants used throughout the generator for maintainability and consistency.
/// </summary>
internal static class Constants
{
    #region Default Configuration

    public const string DefaultNamespace = "MagicConstants";
    public const string DefaultVisibility = "internal";

    #endregion

    #region Template Tokens

    public const string MagicTimeToken = "{MAGIC_TIME}";
    public const string MagicHashToken = "{MAGIC_HASH}";
    public const string MagicFilePattern = @"{MAGIC_FILE\s+(?<file>.+?)\s*}";

    #endregion

    #region Index Files

    public const string IndexHtml = "index.html";
    public const string IndexHtm = "index.htm";
    public const string IndexHtmlPath = "/index.html";
    public const string IndexHtmPath = "/index.htm";

    #endregion

    #region Route Generation

    public const string RouteParameterFormat = "HttpContext context";
    public const string CacheControlHeaderTemplate = @"
                context.Response.Headers.CacheControl = ""{0}"";";

    #endregion

    #region Extension Priorities

    public const int HtmlExtensionPriority = 0;
    public const int CssExtensionPriority = 1;
    public const int JsExtensionPriority = 2;
    public const int DefaultExtensionPriority = 3;

    #endregion

    #region Hash Calculation

    public const int HashSeed = 17;
    public const int HashMultiplier = 31;

    #endregion

    #region Encoding

    public const string EncodingChars = "if1k2dLJHswO3N45";

    #endregion
}
