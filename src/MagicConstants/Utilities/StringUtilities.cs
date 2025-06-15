using MagicConstants.Core;
using System.Text;

namespace MagicConstants.Utilities;

/// <summary>
/// Provides string manipulation and generation utilities.
/// </summary>
internal static class StringUtilities
{
    /// <summary>
    /// Converts a string to a safe identifier by replacing invalid characters with underscores.
    /// </summary>
    /// <param name="value">The value to make safe.</param>
    /// <param name="includeSlashes">Whether to also replace slash characters.</param>
    /// <returns>A safe string suitable for use as an identifier.</returns>
    public static string MakeSafeIdentifier(string? value, bool includeSlashes)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        string result = value!.Replace(".", "_")
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

    /// <summary>
    /// Capitalizes the first character of a string.
    /// </summary>
    /// <param name="input">The input string to capitalize.</param>
    /// <returns>The string with the first character capitalized, or empty string if input is null or empty.</returns>
    public static string CapitalizeFirst(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }
        
        return char.ToUpper(input[0]) + input.Substring(1);
    }

    /// <summary>
    /// Escapes content for use in C# verbatim string literals.
    /// </summary>
    public static string EscapeForCSharpStringLiteral(string content)
    {
        return "@\"" + content.Replace("\"", "\"\"") + "\"";
    }

    /// <summary>
    /// Generates a unique identifier based on the current timestamp using a custom character set.
    /// </summary>
    /// <returns>A unique string identifier.</returns>
    public static string GenerateUniqueIdentifier()
    {
        StringBuilder hash = new();
        long seconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        while (seconds > 0)
        {
            hash.Insert(0, Constants.EncodingChars[(int)(seconds % Constants.EncodingChars.Length)]);
            seconds /= Constants.EncodingChars.Length;
        }

        return hash.ToString();
    }
}
