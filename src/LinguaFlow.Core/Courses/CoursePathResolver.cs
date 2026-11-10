using System.Text.RegularExpressions;

namespace LinguaFlow.Core.Courses;

public sealed class CoursePathResolver
{
    private static readonly Regex Segment = new("^[A-Za-z0-9][A-Za-z0-9._-]*$", RegexOptions.Compiled);

    public static bool IsSafeRelativePath(string? path) =>
        !string.IsNullOrWhiteSpace(path) && !path.StartsWith('/') && !path.Contains('\\') &&
        !path.Contains(':') && !path.Contains('?') && !path.Contains('#') &&
        path.Split('/').All(part => part is not "." and not ".." && Segment.IsMatch(part));

    public static string LanguageDirectory(string languageCode) => languageCode switch
    {
        "en" => "English", "ja" => "Japan", "zh" => "China",
        _ => throw new CourseContentException($"Unsupported language code: {languageCode}")
    };

    public static string CourseRoot(CourseCatalogEntry entry) =>
        $"Courses/{entry.LanguageDirectory}/{entry.CourseId}/";

    public Uri ResolveCatalog(Uri appBase) => new(appBase, "Courses/catalog.json");

    public Uri ResolveManifest(Uri appBase, CourseCatalogEntry entry)
    {
        var root = CourseRoot(entry);
        if (!IsSafeRelativePath(entry.ManifestPath) || entry.ManifestPath != root + "manifest.json")
            throw new CourseContentException("Manifest path must point to its own course directory.");
        return new Uri(appBase, entry.ManifestPath);
    }

    public Uri ResolveContent(Uri manifestUrl, string relativePath)
    {
        if (!IsSafeRelativePath(relativePath)) throw new CourseContentException("Invalid relative content path.");
        var resolved = new Uri(manifestUrl, relativePath);
        var root = manifestUrl.AbsoluteUri[..(manifestUrl.AbsoluteUri.LastIndexOf('/') + 1)];
        if (!resolved.AbsoluteUri.StartsWith(root, StringComparison.Ordinal))
            throw new CourseContentException("Content path escapes its course directory.");
        return resolved;
    }
}

public sealed class CourseContentException(string message, Exception? inner = null) : Exception(message, inner);
