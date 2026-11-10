using System.Net.Http.Json;
using System.Text.Json;
using LinguaFlow.Core.Courses;

namespace LinguaFlow.Web.Services;

public sealed class VocabularyContentService(HttpClient http, CourseContentCache cache, CoursePathResolver paths, CourseContentValidator validator)
{
    public async Task<VocabularyDocument> GetAsync(CourseCatalogEntry entry, CourseManifest manifest, CancellationToken cancellationToken = default)
    {
        if (entry.CourseId != manifest.CourseId || entry.Status != CourseStatus.Available) throw new CourseContentException("Course is unavailable.");
        var reference = manifest.Vocabulary.FirstOrDefault() ?? throw new CourseContentException("Vocabulary reference is missing.");
        var manifestUrl = paths.ResolveManifest(http.BaseAddress ?? throw new CourseContentException("Base address is missing."), entry);
        var uri = paths.ResolveContent(manifestUrl, reference.Path);
        return await cache.GetOrAddAsync(uri, async ct =>
        {
            using var response = await http.GetAsync(uri, ct);
            if (!response.IsSuccessStatusCode) throw new CourseContentException($"Vocabulary file unavailable ({(int)response.StatusCode}).");
            VocabularyDocument? document;
            try { document = await response.Content.ReadFromJsonAsync<VocabularyDocument>(CourseJson.Options, ct); }
            catch (JsonException ex) { throw new CourseContentException("Vocabulary JSON is invalid.", ex); }
            var errors = validator.ValidateVocabulary(document, manifest);
            if (errors.Count > 0) throw new CourseContentException(string.Join(" ", errors));
            return document!;
        }, cancellationToken);
    }
    public Uri? AudioUrl(CourseCatalogEntry entry, VocabularyEntry word)
    {
        if (word.AudioPath is null) return null;
        if (!CoursePathResolver.IsSafeRelativePath(word.AudioPath) || !word.AudioPath.StartsWith("Assets/", StringComparison.Ordinal)) throw new CourseContentException("Unsafe vocabulary audio path.");
        var manifestUrl = paths.ResolveManifest(http.BaseAddress ?? throw new CourseContentException("Base address is missing."), entry);
        return paths.ResolveContent(manifestUrl, word.AudioPath);
    }
}
