using Microsoft.CodeAnalysis;
using MagicConstants.Core;
using MagicConstants.Models;
using MagicConstants.Utilities;
using NUglify;
using System.Text;
using System.Text.RegularExpressions;

namespace MagicConstants.Processing;

/// <summary>
/// Handles the processing of files, including content transformation and minification.
/// </summary>
internal static class FileProcessor
{
    private static readonly Regex s_templateFileRegex = new(Constants.MagicFilePattern, 
        RegexOptions.Compiled | RegexOptions.CultureInvariant);    /// <summary>
    /// Creates a FileData instance from file options and global options.
    /// </summary>
    public static FileData CreateFileData(Models.FileOptions fileOptions, Models.GlobalOptions globalOptions)
    {
        string extension = Path.GetExtension(fileOptions.File.Path)?.ToLowerInvariant() ?? string.Empty;
        string content = ProcessFileContent(fileOptions.File, extension, fileOptions, globalOptions);
        string relativePath = PathUtilities.GetRelativeFilename(fileOptions.File.Path, globalOptions.ProjectDirectory);        
        bool shouldMinify = fileOptions.Minify.HasValue ? fileOptions.Minify.GetValueOrDefault() : globalOptions.Minify;
        
        return new FileData(
            fileOptions.Class!,
            relativePath,
            extension,
            content,
            fileOptions.RemoveRouteExtension,
            fileOptions.CacheControl,
            shouldMinify && FileExtensions.IsMinifiable(extension)
        );
    }    /// <summary>
    /// Processes file content based on its type and configuration options.
    /// </summary>
    private static string ProcessFileContent(AdditionalText file, string extension, Models.FileOptions fileOptions, Models.GlobalOptions globalOptions)
    {
        // Handle binary files
        if (FileExtensions.IsBinary(extension))
        {
            return ProcessBinaryFile(file.Path);
        }

        // Handle text files
        string content = file.GetText()?.ToString() ?? string.Empty;
        content = ProcessTextFile(content, extension, file.Path);
        
        // Apply minification if requested
        content = ApplyMinificationIfNeeded(content, extension, fileOptions, globalOptions);

        // Escape content for C# string literal
        return StringUtilities.EscapeForCSharpStringLiteral(content);
    }

    /// <summary>
    /// Processes binary files by reading bytes and formatting as C# byte array.
    /// </summary>
    private static string ProcessBinaryFile(string filePath)
    {
#pragma warning disable RS1035 // Do not use APIs banned for analyzers
        byte[] bytes = File.ReadAllBytes(filePath);
#pragma warning restore RS1035 // Do not use APIs banned for analyzers
        return "new byte[] { " + string.Join(", ", bytes) + " }";
    }

    /// <summary>
    /// Processes text files by replacing magic tokens and inlining templates.
    /// </summary>
    private static string ProcessTextFile(string content, string extension, string filePath)
    {
        // Process templates if applicable
        if (FileExtensions.IsTextProcessable(extension))
        {
            content = content.Replace(Constants.MagicTimeToken, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
                             .Replace(Constants.MagicHashToken, StringUtilities.GenerateUniqueIdentifier());

            DirectoryInfo directory = new FileInfo(filePath).Directory!;
            content = InlineTemplates(content, directory);
        }

        return content;
    }    /// <summary>
    /// Applies minification to content if configured and supported.
    /// </summary>
    private static string ApplyMinificationIfNeeded(string content, string extension, Models.FileOptions fileOptions, Models.GlobalOptions globalOptions)
    {
        bool shouldMinify = (fileOptions.Minify.HasValue && fileOptions.Minify.Value) || globalOptions.Minify;
        if (shouldMinify && FileExtensions.IsMinifiable(extension))
        {
            content = MinifyContent(content, extension);
        }

        return content;
    }

    /// <summary>
    /// Minifies content based on file extension using NUglify.
    /// </summary>
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

    /// <summary>
    /// Recursively inlines template files referenced in content.
    /// </summary>
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
}
