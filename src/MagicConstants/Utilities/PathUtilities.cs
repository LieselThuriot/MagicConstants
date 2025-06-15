namespace MagicConstants.Utilities;

/// <summary>
/// Provides path manipulation utilities.
/// </summary>
internal static class PathUtilities
{
    /// <summary>
    /// Gets the relative filename from a full path.
    /// </summary>
    public static string GetRelativeFilename(string filePath, string? projectDirectory)
    {
        if (projectDirectory is not null)
        {
            return filePath.Substring(projectDirectory.Length);
        }

        return Path.GetFileName(filePath);
    }
}
