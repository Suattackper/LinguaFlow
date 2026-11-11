using LinguaFlow.Core.Courses;

namespace LinguaFlow.Web.Services;

public static class VocabularyUnitFilter
{
    public static string Resolve(string url, CourseManifest manifest)
    {
        var query = new Uri(url).Query.TrimStart('?');
        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length != 2 || !string.Equals(pair[0], "unit", StringComparison.OrdinalIgnoreCase)) continue;
            var candidate = Uri.UnescapeDataString(pair[1].Replace('+', ' '));
            return manifest.Units.Any(u => u.UnitId == candidate) ? candidate : "";
        }
        return "";
    }
}
