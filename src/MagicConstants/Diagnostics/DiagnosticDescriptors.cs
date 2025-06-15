using Microsoft.CodeAnalysis;

namespace MagicConstants.Diagnostics;

/// <summary>
/// Provides diagnostic descriptors for the source generator.
/// </summary>
internal static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor FileProcessingError = new(
        "MG0001",
        "Error processing file",
        "Error processing file '{0}': {1}",
        "MagicConstants",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor RouteGenerationError = new(
        "MG0002",
        "Error generating route",
        "Error generating route for file '{0}': {1}",
        "MagicConstants",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
