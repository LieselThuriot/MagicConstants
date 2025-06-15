using Microsoft.CodeAnalysis;

namespace MagicConstants.Models;

/// <summary>
/// File-specific configuration options.
/// </summary>
internal readonly record struct FileOptions(
    string? Class, 
    bool RemoveRouteExtension, 
    AdditionalText File, 
    string? CacheControl, 
    bool? Minify);
