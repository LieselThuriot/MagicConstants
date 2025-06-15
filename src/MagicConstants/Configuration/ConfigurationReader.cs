using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using MagicConstants.Core;
using MagicConstants.Models;

namespace MagicConstants.Configuration;

/// <summary>
/// Responsible for reading configuration from analyzer options and metadata.
/// </summary>
internal static class ConfigurationReader
{    /// <summary>
    /// Creates global options from analyzer configuration.
    /// </summary>
    public static Models.GlobalOptions CreateGlobalOptions(AnalyzerConfigOptionsProvider provider)
    {
        return new Models.GlobalOptions(
            Namespace: GetGlobalOption(provider, "rootnamespace") ?? Constants.DefaultNamespace,
            Visibility: GetGlobalOption(provider, "MagicConstantsVisibility") ?? Constants.DefaultVisibility,
            Routes: TryParseBool(GetGlobalOption(provider, "MagicConstantsRoutes")),
            CacheControl: GetGlobalOption(provider, "MagicConstantsRoutesCacheControl"),
            Minify: TryParseBool(GetGlobalOption(provider, "MagicConstantsMinify")),
            ProjectDirectory: GetGlobalOption(provider, "projectdir")
        );
    }/// <summary>
    /// Extracts file-specific options from additional text metadata.
    /// </summary>
    public static Models.FileOptions ExtractFileOptions(AdditionalText file, AnalyzerConfigOptionsProvider provider)
    {
        string? className = GetAdditionalFileMetadata(provider, file, "MagicClass");
        bool removeRouteExtension = TryParseBool(GetAdditionalFileMetadata(provider, file, "MagicRemoveRouteExtension"));
        string? cacheControl = GetAdditionalFileMetadata(provider, file, "MagicCacheControl");
        
        string? shouldMinifyString = GetAdditionalFileMetadata(provider, file, "MagicMinify");
        bool? shouldMinify = string.IsNullOrEmpty(shouldMinifyString) ? null : TryParseBool(shouldMinifyString);

        return new Models.FileOptions(className, removeRouteExtension, file, cacheControl, shouldMinify);
    }

    /// <summary>
    /// Gets a global configuration option value.
    /// </summary>
    private static string? GetGlobalOption(AnalyzerConfigOptionsProvider provider, string name)
    {
        if (provider.GlobalOptions.TryGetValue($"build_property.{name}", out string? value))
        {
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets metadata for an additional file.
    /// </summary>
    private static string? GetAdditionalFileMetadata(AnalyzerConfigOptionsProvider provider, AdditionalText file, string name)
    {
        if (provider.GetOptions(file).TryGetValue($"build_metadata.AdditionalFiles.{name}", out string? value))
        {
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>
    /// Helper method to safely parse boolean values.
    /// </summary>
    private static bool TryParseBool(string? value)
    {
        return !string.IsNullOrEmpty(value) && bool.TryParse(value, out bool result) && result;
    }
}
