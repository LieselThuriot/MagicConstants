using Microsoft.CodeAnalysis;
using MagicConstants.Configuration;
using MagicConstants.Generation;
using MagicConstants.Models;
using MagicConstants.Processing;

namespace MagicConstants;

/// <summary>
/// Incremental source generator that creates constants from additional files and optionally generates routes.
/// Optimized following Andrew Lock's performance best practices for incremental generators.
/// </summary>
[Generator]
public class Generator : IIncrementalGenerator
{
    /// <summary>
    /// Initializes the incremental generator with the required providers and output registrations.
    /// </summary>
    /// <param name="context">The initialization context.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Performance optimization: Create separate providers for better caching
        IncrementalValueProvider<GlobalOptions> globalOptions = CreateGlobalOptionsProvider(context);

        // Performance optimization: Extract data model early in the pipeline for better caching
        IncrementalValuesProvider<FileData> fileDataProvider = CreateFileDataProvider(context, globalOptions);

        // Generate source files
        context.RegisterSourceOutput(fileDataProvider, SourceGenerators.GenerateFileSource);

        // Generate routes if enabled
        IncrementalValuesProvider<FileData> routeEnabledFiles = fileDataProvider
            .Combine(globalOptions)
            .Where(static pair => pair.Right.Routes)
            .Select(static (pair, _) => pair.Left);
            
        context.RegisterSourceOutput(routeEnabledFiles, SourceGenerators.GenerateRouteSource);
        context.RegisterSourceOutput(routeEnabledFiles.Collect().Combine(globalOptions), SourceGenerators.GenerateRouteCollection);
    }

    /// <summary>
    /// Creates the global options provider from analyzer configuration.
    /// </summary>
    private static IncrementalValueProvider<GlobalOptions> CreateGlobalOptionsProvider(IncrementalGeneratorInitializationContext context)
    {
        return context.AnalyzerConfigOptionsProvider.Select(static (provider, _) =>
            ConfigurationReader.CreateGlobalOptions(provider));
    }

    /// <summary>
    /// Creates the file data provider by transforming additional texts.
    /// </summary>
    private static IncrementalValuesProvider<FileData> CreateFileDataProvider(
        IncrementalGeneratorInitializationContext context, 
        IncrementalValueProvider<GlobalOptions> globalOptions)
    {
        // Performance optimization: Transform to data model early for caching
        return context.AdditionalTextsProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select(static (pair, _) => ConfigurationReader.ExtractFileOptions(pair.Left, pair.Right))
            .Where(static fileOptions => fileOptions.Class is not null)
            .Combine(globalOptions)
            .Select(static (combined, _) => FileProcessor.CreateFileData(combined.Left, combined.Right));
    }
}