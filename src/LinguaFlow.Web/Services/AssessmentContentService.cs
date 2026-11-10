using System.Net.Http.Json;
using System.Text.Json;
using LinguaFlow.Core.Courses;

namespace LinguaFlow.Web.Services;

public sealed class AssessmentContentService(HttpClient http, CourseContentCache cache, CoursePathResolver paths)
{
    public async Task<AssessmentDocument> GetForUnitAsync(CourseCatalogEntry entry, CourseManifest manifest, UnitContent unit, CancellationToken cancellationToken = default)
    {
        var reference = manifest.Assessments.FirstOrDefault() ?? throw new CourseContentException("Assessment reference is missing.");
        var manifestUri = paths.ResolveManifest(http.BaseAddress ?? throw new CourseContentException("Base URL is missing."), entry);
        var uri = paths.ResolveContent(manifestUri, reference.Path);
        var document = await cache.GetOrAddAsync(uri, async ct =>
        {
            using var response = await http.GetAsync(uri, ct);
            if (!response.IsSuccessStatusCode) throw new CourseContentException($"Assessment data unavailable ({(int)response.StatusCode}).");
            try { return await response.Content.ReadFromJsonAsync<AssessmentDocument>(CourseJson.Options, ct) ?? throw new CourseContentException("Assessment data is empty."); }
            catch (JsonException ex) { throw new CourseContentException("Assessment JSON is invalid.", ex); }
        }, cancellationToken);
        if (document.SchemaVersion != 1 || document.CourseId != manifest.CourseId || document.Answers is null || document.Rubrics is null || document.Thresholds is null) throw new CourseContentException("Assessment schema or course reference is invalid.");
        var questionIds = unit.Sections.SelectMany(s => s.Blocks).OfType<QuestionBlock>().Select(q => q.ExerciseId).ToHashSet();
        var keys = document.Answers.Where(a => a.UnitId == unit.UnitId).ToList();
        if (keys.GroupBy(a => a.ExerciseId).Any(g => g.Count() > 1) || keys.Any(a => !questionIds.Contains(a.ExerciseId))) throw new CourseContentException("Assessment keys do not match this Unit.");
        return document;
    }
}
