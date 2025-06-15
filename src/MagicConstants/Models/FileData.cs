namespace MagicConstants.Models;

/// <summary>
/// Represents file data used in the generation pipeline with value semantics for optimal caching.
/// </summary>
internal readonly record struct FileData(
    string ClassName,
    string RelativePath,
    string Extension,
    string Content,
    bool RemoveRouteExtension,
    string? CacheControl,
    bool ShouldMinify);
