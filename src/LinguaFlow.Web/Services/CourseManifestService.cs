using System.Net.Http.Json;
using System.Text.Json;
using LinguaFlow.Core.Courses;

namespace LinguaFlow.Web.Services;

public sealed class CourseManifestService(HttpClient http, CourseContentCache cache, CoursePathResolver paths, CourseContentValidator validator)
{
    public Task<CourseManifest> GetManifestAsync(CourseCatalogEntry entry, CancellationToken cancellationToken = default)
    {
        if (entry.Status != CourseStatus.Available) throw new CourseContentException("Course is not available.");
        var uri = paths.ResolveManifest(http.BaseAddress ?? throw new CourseContentException("App base address is missing."), entry);
        return cache.GetOrAddAsync(uri, async ct =>
        {
            using var response = await http.GetAsync(uri, ct);
            if (!response.IsSuccessStatusCode) throw new CourseContentException($"Course manifest unavailable ({(int)response.StatusCode}).");
            try
            {
                var manifest = await response.Content.ReadFromJsonAsync<CourseManifest>(CourseJson.Options, ct);
                var errors = validator.ValidateManifest(manifest, entry);
                if (errors.Count > 0) throw new CourseContentException(string.Join(" ", errors));
                return manifest!;
            }
            catch (JsonException ex) { throw new CourseContentException("Course manifest JSON is invalid.", ex); }
        }, cancellationToken);
    }
}
