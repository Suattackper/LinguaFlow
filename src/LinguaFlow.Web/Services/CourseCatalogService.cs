using System.Net.Http.Json;
using System.Text.Json;
using LinguaFlow.Core.Courses;

namespace LinguaFlow.Web.Services;

public sealed class CourseCatalogService(HttpClient http, CourseContentCache cache, CoursePathResolver paths, CourseContentValidator validator)
{
    public Task<CourseCatalog> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        var uri = paths.ResolveCatalog(http.BaseAddress ?? throw new CourseContentException("App base address is missing."));
        return cache.GetOrAddAsync(uri, async ct =>
        {
            using var response = await http.GetAsync(uri, ct);
            if (!response.IsSuccessStatusCode) throw new CourseContentException($"Catalog unavailable ({(int)response.StatusCode}).");
            try
            {
                var catalog = await response.Content.ReadFromJsonAsync<CourseCatalog>(CourseJson.Options, ct);
                var errors = validator.ValidateCatalog(catalog);
                if (errors.Count > 0) throw new CourseContentException(string.Join(" ", errors));
                return catalog!;
            }
            catch (JsonException ex) { throw new CourseContentException("Catalog JSON is invalid.", ex); }
        }, cancellationToken);
    }

    public async Task<CourseCatalogEntry?> FindAvailableAsync(string courseId, CancellationToken cancellationToken = default) =>
        (await GetCatalogAsync(cancellationToken)).Courses.FirstOrDefault(x => x.CourseId == courseId && x.Status == CourseStatus.Available);
}
