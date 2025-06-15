using MagicConstants.Utilities;

namespace MagicConstants.Templates;

/// <summary>
/// Provides code generation templates for files and routes.
/// </summary>
internal static class CodeTemplates
{
    /// <summary>
    /// Generates the template for file constant classes.
    /// </summary>
    public static string GenerateFileTemplate(string content, string type, string className, string namespaceName, string visibility, string filename)
    {
        string[] inners = filename.Split('/');
        string result = $@"
public {type} {StringUtilities.CapitalizeFirst(inners.Last())} = {content};
";

        for (int i = inners.Length - 2; i >= 0; i--)
        {
            result = $@"
    {visibility} static partial class {StringUtilities.CapitalizeFirst(inners[i])}
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

    /// <summary>
    /// Generates route mapping source code.
    /// </summary>
    public static string GenerateRouteSourceCode(string className, string filename, string routeName, string parameters, string cacheControl, string extension)
    {
        string methodName = StringUtilities.CapitalizeFirst(StringUtilities.MakeSafeIdentifier(filename, includeSlashes: true).Replace('/', '_'));
        string propertyPath = string.Join(".", filename.Split('/').Select(StringUtilities.CapitalizeFirst));
        
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
                return TypedResults.Text({className}.{propertyPath}, {RouteUtilities.GetMimeType(extension)});
            }}); 
        }}
    }}
}}";
    }

    /// <summary>
    /// Generates the route collection source code.
    /// </summary>
    public static string GenerateRouteCollectionSource(string namespaceName, string visibility, IEnumerable<string> filenames)
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
}
