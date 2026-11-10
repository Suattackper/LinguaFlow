using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using LinguaFlow.Core.Courses;

namespace LinguaFlow.Web.Services;

public sealed class UnitContentService(HttpClient http, CourseContentCache cache, CoursePathResolver paths, CourseContentValidator validator)
{
    public Task<UnitLoadResult> GetUnitAsync(CourseCatalogEntry entry, CourseManifest manifest, string unitId, CancellationToken cancellationToken = default)
    {
        if (entry.Status != CourseStatus.Available || entry.CourseId != manifest.CourseId) throw new CourseContentException("Course is not available or manifest does not match.");
        var reference = manifest.Units.FirstOrDefault(x => x.UnitId == unitId) ?? throw new CourseContentException("Unit is not listed in the manifest.");
        var manifestUrl = paths.ResolveManifest(http.BaseAddress ?? throw new CourseContentException("App base address is missing."), entry);
        var uri = paths.ResolveContent(manifestUrl, reference.Path);
        return cache.GetOrAddAsync(uri, async ct =>
        {
            using var response = await http.GetAsync(uri, ct);
            if (!response.IsSuccessStatusCode) throw new CourseContentException($"Unit file unavailable ({(int)response.StatusCode}).");
            try
            {
                var node = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: ct) ?? throw new CourseContentException("Unit file is empty.");
                var warnings = new List<string>();
                if (node["sections"] is JsonArray sections)
                {
                    foreach (var section in sections)
                    {
                        if (section?["blocks"] is not JsonArray blocks) continue;
                        for (var i = blocks.Count - 1; i >= 0; i--)
                        {
                            var block = blocks[i];
                            var type = block?["type"]?.GetValue<string>();
                            if (KnownTypes.Contains(type)) continue;
                            if (block?["optional"]?.GetValue<bool>() == true)
                            {
                                warnings.Add($"Skipped unknown optional block type: {type}");
                                blocks.RemoveAt(i);
                            }
                            else throw new CourseContentException($"Unsupported required block type: {type}");
                        }
                    }
                }
                var unit = node.Deserialize<UnitContent>(CourseJson.Options);
                var errors = validator.ValidateUnit(unit, manifest, unitId);
                if (errors.Count > 0) throw new CourseContentException(string.Join(" ", errors));
                return new UnitLoadResult(unit!, warnings);
            }
            catch (JsonException ex) { throw new CourseContentException("Unit JSON is invalid.", ex); }
        }, cancellationToken);
    }

    private static readonly HashSet<string?> KnownTypes = ["heading", "explanation", "grammar", "example", "dialogue", "reading", "vocabulary", "question", "exercise-group", "writing", "speaking", "listening", "homework", "review", "image", "audio"];
}

public sealed record UnitLoadResult(UnitContent Content, IReadOnlyList<string> Warnings);
