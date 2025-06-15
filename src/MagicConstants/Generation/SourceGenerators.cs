using Microsoft.CodeAnalysis;
using MagicConstants.Core;
using MagicConstants.Diagnostics;
using MagicConstants.Models;
using MagicConstants.Templates;
using MagicConstants.Utilities;
using System.Collections.Immutable;

namespace MagicConstants.Generation;

/// <summary>
/// Handles the actual source code generation for files and routes.
/// </summary>
internal static class SourceGenerators
{
    /// <summary>
    /// Generates source code for individual files.
    /// </summary>
    public static void GenerateFileSource(SourceProductionContext spc, FileData fileData)
    {
        try
        {
            string safeFilename = StringUtilities.MakeSafeIdentifier(fileData.RelativePath, includeSlashes: false);
            string contentType = FileExtensions.IsBinary(fileData.Extension) ? "static readonly byte[]" : "const string";
            
            spc.AddSource(
                hintName: $"{fileData.ClassName}.{safeFilename.Replace('/', '_')}.g.cs",
                source: CodeTemplates.GenerateFileTemplate(fileData.Content, contentType, fileData.ClassName, Constants.DefaultNamespace, Constants.DefaultVisibility, safeFilename));
        }
        catch (Exception ex)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.FileProcessingError,
                location: null,
                fileData.RelativePath,
                ex.Message));
        }
    }

    /// <summary>
    /// Generates route source code for individual files.
    /// </summary>
    public static void GenerateRouteSource(SourceProductionContext spc, FileData fileData)
    {
        try
        {
            string routeName = RouteUtilities.NormalizeRouteName(fileData.RelativePath, fileData.Extension, fileData.RemoveRouteExtension);
            string safeMethodName = StringUtilities.MakeSafeIdentifier(fileData.RelativePath, includeSlashes: true);
            string parameters = string.IsNullOrEmpty(fileData.CacheControl) ? string.Empty : Constants.RouteParameterFormat;
            string cacheControl = string.IsNullOrEmpty(fileData.CacheControl) ? string.Empty : 
                string.Format(Constants.CacheControlHeaderTemplate, fileData.CacheControl);

            spc.AddSource(
                hintName: $"Routes.{fileData.ClassName}.{safeMethodName.Replace('/', '_')}.g.cs",
                source: CodeTemplates.GenerateRouteSourceCode(fileData.ClassName, fileData.RelativePath, routeName, parameters, cacheControl, fileData.Extension));
        }
        catch (Exception ex)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.RouteGenerationError,
                location: null,
                fileData.RelativePath,
                ex.Message));
        }
    }    /// <summary>
    /// Generates the route collection source code.
    /// </summary>
    public static void GenerateRouteCollection(SourceProductionContext spc, (ImmutableArray<FileData> Files, Models.GlobalOptions GlobalOptions) combined)
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
                SafeName = StringUtilities.MakeSafeIdentifier(fileData.RelativePath, includeSlashes: true)
            })
            .OrderBy(item => FileExtensions.GetPriority(item.Extension))
            .ThenBy(item => item.FileData.RelativePath.Count(x => x == '/'))
            .Select(item => $"            app.Map{StringUtilities.CapitalizeFirst(item.SafeName)}();");

        spc.AddSource(
            hintName: "Routes.g.cs",
            source: CodeTemplates.GenerateRouteCollectionSource(globalOptions.Namespace, globalOptions.Visibility, sortedFilenames));
    }
}
