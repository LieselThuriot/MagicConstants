namespace MagicConstants.Models;

/// <summary>
/// Global configuration options.
/// </summary>
internal readonly record struct GlobalOptions(
    string Namespace, 
    string Visibility, 
    bool Routes, 
    string? CacheControl, 
    bool Minify, 
    string? ProjectDirectory);
