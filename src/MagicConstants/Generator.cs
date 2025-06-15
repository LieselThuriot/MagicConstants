using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUglify;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using FileOptions = (string? Class, bool RemoveRouteExtension, Microsoft.CodeAnalysis.AdditionalText File, string? CacheControl, bool? Minify);
using GlobalOptions = (string Namespace, string Visibility, bool Routes, string? CacheControl, bool Minify, string? ProjectDirectory);

namespace MagicConstants;

/// <summary>
/// Incremental source generator that creates constants from additional files and optionally generates routes.
/// Optimized following Andrew Lock's performance best practices for incremental generators.
/// </summary>
[Generator]
public class Generator : IIncrementalGenerator
{
    // Performance optimization: Cache compiled regex patterns and collections
    private static readonly Regex s_templateFileRegex = new(@"{MAGIC_FILE\s+(?<file>.+?)\s*}", 
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    
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

    // Data model for better caching - using struct for value semantics
    private readonly struct FileData : IEquatable<FileData>
    {
        public readonly string ClassName;
        public readonly string RelativePath;
        public readonly string Extension;
        public readonly string Content;
        public readonly bool RemoveRouteExtension;
        public readonly string? CacheControl;
        public readonly bool ShouldMinify;

        public FileData(string className, string relativePath, string extension, string content, 
                       bool removeRouteExtension, string? cacheControl, bool shouldMinify)
        {
            ClassName = className ?? string.Empty;
            RelativePath = relativePath ?? string.Empty;
            Extension = extension ?? string.Empty;
            Content = content ?? string.Empty;
            RemoveRouteExtension = removeRouteExtension;
            CacheControl = cacheControl;
            ShouldMinify = shouldMinify;
        }

        public bool Equals(FileData other)
        {
            return ClassName == other.ClassName &&
                   RelativePath == other.RelativePath &&
                   Extension == other.Extension &&
                   Content == other.Content &&
                   RemoveRouteExtension == other.RemoveRouteExtension &&
                   CacheControl == other.CacheControl &&
                   ShouldMinify == other.ShouldMinify;
        }

        public override bool Equals(object? obj) => obj is FileData other && Equals(other);        public override int GetHashCode()
        {
            // Simple hash combining for netstandard2.0 compatibility
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + (ClassName?.GetHashCode() ?? 0);
                hash = (hash * 31) + (RelativePath?.GetHashCode() ?? 0);
                hash = (hash * 31) + (Extension?.GetHashCode() ?? 0);
                hash = (hash * 31) + (Content?.GetHashCode() ?? 0);
                hash = (hash * 31) + RemoveRouteExtension.GetHashCode();
                hash = (hash * 31) + (CacheControl?.GetHashCode() ?? 0);
                hash = (hash * 31) + ShouldMinify.GetHashCode();
                return hash;
            }
        }
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Performance optimization: Create separate providers for better caching
        IncrementalValueProvider<GlobalOptions> globalOptions = CreateGlobalOptionsProvider(context);

        // Performance optimization: Extract data model early in the pipeline for better caching
        IncrementalValuesProvider<FileData> fileDataProvider = CreateFileDataProvider(context, globalOptions);        // Generate source files
        context.RegisterSourceOutput(fileDataProvider, GenerateFileSource);

        // Generate routes if enabled
        IncrementalValuesProvider<FileData> routeEnabledFiles = fileDataProvider
            .Combine(globalOptions)
            .Where(static pair => pair.Right.Routes)
            .Select(static (pair, _) => pair.Left);
            
        context.RegisterSourceOutput(routeEnabledFiles, GenerateRouteSource);
        context.RegisterSourceOutput(routeEnabledFiles.Collect().Combine(globalOptions), GenerateRouteCollection);
    }

    private static IncrementalValueProvider<GlobalOptions> CreateGlobalOptionsProvider(IncrementalGeneratorInitializationContext context)
    {
        return context.AnalyzerConfigOptionsProvider.Select(static (provider, _) =>
        (
            Namespace: GetGlobalOption(provider, "rootnamespace") ?? "MagicConstants",
            Visibility: GetGlobalOption(provider, "MagicConstantsVisibility") ?? "internal",
            Routes: TryParseBool(GetGlobalOption(provider, "MagicConstantsRoutes")),
            CacheControl: GetGlobalOption(provider, "MagicConstantsRoutesCacheControl"),
            Minify: TryParseBool(GetGlobalOption(provider, "MagicConstantsMinify")),
            ProjectDirectory: GetGlobalOption(provider, "projectdir")
        ));
    }

    private static IncrementalValuesProvider<FileData> CreateFileDataProvider(
        IncrementalGeneratorInitializationContext context, 
        IncrementalValueProvider<GlobalOptions> globalOptions)
    {
        // Performance optimization: Transform to data model early for caching
        return context.AdditionalTextsProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select(static (pair, _) => ExtractFileOptions(pair.Left, pair.Right))
            .Where(static fileOptions => fileOptions.Class is not null)
            .Combine(globalOptions)
            .Select(static (combined, _) => CreateFileData(combined.Left, combined.Right));
    }

    private static FileOptions ExtractFileOptions(AdditionalText file, AnalyzerConfigOptionsProvider provider)
    {
        string? className = GetAdditionalFileMetadata(provider, file, "MagicClass");
        bool removeRouteExtension = TryParseBool(GetAdditionalFileMetadata(provider, file, "MagicRemoveRouteExtension"));
        string? cacheControl = GetAdditionalFileMetadata(provider, file, "MagicCacheControl");
        
        string? shouldMinifyString = GetAdditionalFileMetadata(provider, file, "MagicMinify");
        bool? shouldMinify = string.IsNullOrEmpty(shouldMinifyString) ? null : TryParseBool(shouldMinifyString);

        return (Class: className, RemoveRouteExtension: removeRouteExtension, File: file, CacheControl: cacheControl, Minify: shouldMinify);
    }

    private static FileData CreateFileData(FileOptions fileOptions, GlobalOptions globalOptions)
    {
        string extension = Path.GetExtension(fileOptions.File.Path)?.ToLowerInvariant() ?? string.Empty;
        string content = ProcessFileContent(fileOptions.File, extension, fileOptions, globalOptions);
        string relativePath = GetRelativeFilename(fileOptions.File.Path, globalOptions.ProjectDirectory);
        
        bool shouldMinify = (fileOptions.Minify.HasValue && fileOptions.Minify.Value) || globalOptions.Minify;
        
        return new FileData(
            className: fileOptions.Class!,
            relativePath: relativePath,
            extension: extension,
            content: content,
            removeRouteExtension: fileOptions.RemoveRouteExtension,
            cacheControl: fileOptions.CacheControl,
            shouldMinify: shouldMinify && s_minifiableExtensions.Contains(extension)
        );
    }

    // Helper method to safely parse boolean values
    private static bool TryParseBool(string? value)
    {
        return !string.IsNullOrEmpty(value) && bool.TryParse(value, out bool result) && result;
    }

    private static string GetRelativeFilename(string filePath, string? projectDirectory)
    {
        if (projectDirectory is not null)
        {
            return filePath.Substring(projectDirectory.Length);
        }

        return Path.GetFileName(filePath);
    }

    private static string ProcessFileContent(AdditionalText file, string extension, FileOptions fileOptions, GlobalOptions globalOptions)
    {
        string content;

        // Handle binary files
        if (s_binaryExtensions.Contains(extension))
        {
#pragma warning disable RS1035 // Do not use APIs banned for analyzers
            byte[] bytes = File.ReadAllBytes(file.Path);
#pragma warning restore RS1035 // Do not use APIs banned for analyzers
            return "new byte[] { " + string.Join(", ", bytes) + " }";
        }

        // Handle text files
        content = file.GetText()?.ToString() ?? string.Empty;

        // Process templates if applicable
        if (s_textProcessableExtensions.Contains(extension))
        {
            content = content.Replace("{MAGIC_TIME}", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
                             .Replace("{MAGIC_HASH}", GenerateUniqueIdentifier());

            DirectoryInfo directory = new FileInfo(file.Path).Directory!;
            content = InlineTemplates(content, directory);
        }

        // Minify if requested
        bool shouldMinify = (fileOptions.Minify.HasValue && fileOptions.Minify.Value) || globalOptions.Minify;
        if (shouldMinify && s_minifiableExtensions.Contains(extension))
        {
            content = MinifyContent(content, extension);
        }

        // Escape content for C# string literal
        return "@\"" + content.Replace("\"", "\"\"") + "\"";
    }

    private static string MinifyContent(string content, string extension)
    {
        try
        {
            if (extension is ".html" or ".htm")
            {
                var result = Uglify.Html(content, new NUglify.Html.HtmlSettings { ShortBooleanAttribute = false });
                return result.HasErrors ? content : result.Code;
            }
            
            if (extension == ".css")
            {
                var result = Uglify.Css(content);
                return result.HasErrors ? content : result.Code;
            }
            
            if (extension == ".js")
            {
                var result = Uglify.Js(content);
                return result.HasErrors ? content : result.Code;
            }
        }
        catch
        {
            // If minification fails, return original content
        }

        return content;
    }

    private static void GenerateFileSource(SourceProductionContext spc, FileData fileData)
    {
        try
        {
            string safeFilename = SafeString(fileData.RelativePath, includeSlashes: false);
            string contentType = s_binaryExtensions.Contains(fileData.Extension) ? "static readonly byte[]" : "const string";
            spc.AddSource(
                hintName: $"{fileData.ClassName}.{safeFilename.Replace('/', '_')}.g.cs",
                source: GetFileTemplate(fileData.Content, contentType, fileData.ClassName, "MagicConstants", "internal", safeFilename));
        }
        catch (Exception ex)
        {
            // Add diagnostic for any errors during source generation
            spc.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "MG0001",
                    "Error processing file",
                    "Error processing file '{0}': {1}",
                    "MagicConstants",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true),
                location: null,
                fileData.RelativePath,                ex.Message));
        }
    }

    private static void GenerateRouteSource(SourceProductionContext spc, FileData fileData)
    {
        try
        {
            string routeName = NormalizeRouteName(fileData.RelativePath, fileData.Extension, fileData.RemoveRouteExtension);
            string safeMethodName = SafeString(fileData.RelativePath, includeSlashes: true);
            string parameters = string.IsNullOrEmpty(fileData.CacheControl) ? string.Empty : "HttpContext context";
            string cacheControl = string.IsNullOrEmpty(fileData.CacheControl) ? string.Empty : 
                $@"
                context.Response.Headers.CacheControl = ""{fileData.CacheControl}"";";

            spc.AddSource(
                hintName: $"Routes.{fileData.ClassName}.{safeMethodName.Replace('/', '_')}.g.cs",
                source: GenerateRouteSourceCode(fileData.ClassName, fileData.RelativePath, routeName, parameters, cacheControl, fileData.Extension));
        }
        catch (Exception ex)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "MG0002",
                    "Error generating route",
                    "Error generating route for file '{0}': {1}",
                    "MagicConstants",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true),
                location: null,
                fileData.RelativePath,
                ex.Message));
        }
    }    private static void GenerateRouteCollection(SourceProductionContext spc, (ImmutableArray<FileData> Files, GlobalOptions GlobalOptions) combined)
    {
        var (files, globalOptions) = combined;

        if (files.IsEmpty || !globalOptions.Routes)
        {
            return;
        }

        var sortedFilenames = files
            .Select(fileData => new 
            { 
                FileData = fileData,
                Extension = fileData.Extension,
                SafeName = SafeString(fileData.RelativePath, includeSlashes: true)
            })
            .OrderBy(item => GetExtensionPriority(item.Extension))
            .ThenBy(item => item.FileData.RelativePath.Count(x => x == '/'))
            .Select(item => $"            app.Map{FirstToUpper(item.SafeName)}();");

        spc.AddSource(
            hintName: "Routes.g.cs",
            source: GenerateRouteCollectionSource(globalOptions.Namespace, globalOptions.Visibility, sortedFilenames));
    }

    private static string NormalizeRouteName(string filename, string extension, bool removeRouteExtension)
    {
        string routeName = filename.Replace("\\", "/");
        
        if (routeName is "index.html" or "index.htm")
        {
            return string.Empty;
        }
        
        if (routeName.EndsWith("/index.htm"))
        {
            return routeName.Substring(0, routeName.Length - "/index.htm".Length);
        }
        
        if (routeName.EndsWith("/index.html"))
        {
            return routeName.Substring(0, routeName.Length - "/index.html".Length);
        }
        
        if (removeRouteExtension)
        {
            return routeName.Substring(0, routeName.Length - extension.Length);
        }
        
        return routeName;
    }

    private static string GenerateRouteSourceCode(string className, string filename, string routeName, string parameters, string cacheControl, string extension)
    {
        string methodName = FirstToUpper(SafeString(filename, includeSlashes: true).Replace('/', '_'));
        string propertyPath = string.Join(".", filename.Split('/').Select(FirstToUpper));
        
        return $@"using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Net.Mime;
#nullable enable

namespace MagicConstants
{{
    internal static partial class Routes
    {{
        public static void Map{methodName}(this IEndpointRouteBuilder app)
        {{
            app.Map{methodName}(""{routeName}"");
        }}

        public static void Map{methodName}(this IEndpointRouteBuilder app, string route)
        {{
            app.MapGet(""/"" + route, ({parameters}) =>
            {{{cacheControl}
                return TypedResults.Text({className}.{propertyPath}, {GetMimeType(extension)});
            }}); 
        }}
    }}
}}";
    }

    private static int GetExtensionPriority(string extension)
    {
        return extension switch
        {
            ".html" or ".htm" => 0,
            ".css" => 1,
            ".js" => 2,
            _ => 3
        };
    }

    private static string GenerateRouteCollectionSource(string namespaceName, string visibility, IEnumerable<string> filenames)
    {
        return $@"using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Net.Mime;
#nullable enable

namespace {namespaceName}
{{
    {visibility} static partial class Routes
    {{
        public static void MapViews(this IEndpointRouteBuilder app)
        {{
{string.Join("\r\n", filenames)}
        }}
    }}
}}";
    }

    private static string GetMimeType(string extension)
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

    private static string SafeString(string? value, bool includeSlashes)
    {
        if (value is null)
        {
            return string.Empty;
        }

        string result = value.Replace(".", "_")
                             .Replace("-", "_")
                             .Replace("{", "_")
                             .Replace("}", "_")
                             .Replace(" ", "_");

        if (includeSlashes)
        {
            result = result.Replace("/", "_").Replace("\\", "_");
        }

        return result;
    }

    private static string InlineTemplates(string content, DirectoryInfo directory)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        return s_templateFileRegex.Replace(content, match =>
        {
            string fileName = match.Groups["file"].Value;

            FileInfo file = Path.IsPathRooted(fileName)
                ? new(fileName)
                : new(Path.Combine(directory.FullName, fileName));

            if (!file.Exists)
            {
                return match.Value;
            }

#pragma warning disable RS1035 // Do not use APIs banned for analyzers
            string fileContent = File.ReadAllText(file.FullName);
#pragma warning restore RS1035 // Do not use APIs banned for analyzers

            return InlineTemplates(fileContent, file.Directory!);
        });
    }

    private static string GenerateUniqueIdentifier()
    {
        StringBuilder hash = new();
        long seconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        const string chars = "if1k2dLJHswO3N45";

        while (seconds > 0)
        {
            hash.Insert(0, chars[(int)(seconds % chars.Length)]);
            seconds /= chars.Length;
        }

        return hash.ToString();
    }

    private static string GetFileTemplate(string content, string type, string className, string namespaceName, string visibility, string filename)
    {
        string[] inners = filename.Split('/');
        string result = $@"
public {type} {FirstToUpper(inners.Last())} = {content};
";

        for (int i = inners.Length - 2; i >= 0; i--)
        {
            result = $@"
    {visibility} static partial class {FirstToUpper(inners[i])}
    {{
        {result}
    }}
";
        }

        return $@"namespace {namespaceName}
{{
    {visibility} static partial class {className}
    {{
        {result}
    }}
}}
";
    }

    private static string FirstToUpper(string filename) => char.ToUpper(filename[0]) + filename.Substring(1);
}