using System.Net.Http.Json;
using LinguaFlow.Core.Courses;

namespace LinguaFlow.Web.Services;

public sealed class PracticeContentService(HttpClient http, CourseContentCache cache, CoursePathResolver paths, CourseContentValidator validator)
{
    public async Task<PracticeConfiguration> GetAsync(CourseCatalogEntry entry, CourseManifest manifest, CancellationToken cancellationToken = default)
    {
        if (entry.CourseId != manifest.CourseId || entry.Status != CourseStatus.Available) throw new CourseContentException("Course mismatch.");
        var reference = manifest.AiPractice.FirstOrDefault() ?? throw new CourseContentException("Practice instructions are unavailable.");
        var uri = paths.ResolveContent(paths.ResolveManifest(http.BaseAddress ?? throw new CourseContentException("Base URL missing."), entry), reference.Path);
        return await cache.GetOrAddAsync(uri, async ct =>
        {
            using var response = await http.GetAsync(uri, ct);
            if (!response.IsSuccessStatusCode) throw new CourseContentException("Practice instructions are unavailable.");
            var document = await response.Content.ReadFromJsonAsync<PracticeConfiguration>(CourseJson.Options, ct);
            var errors = validator.ValidatePractice(document, manifest);
            if (errors.Count > 0) throw new CourseContentException(string.Join(" ", errors));
            return document!;
        }, cancellationToken);
    }
}
