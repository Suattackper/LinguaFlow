using System.Net;
using System.Text;
using System.Text.Json;
using LinguaFlow.Core.Courses;
using LinguaFlow.Web.Services;
using Xunit;

namespace LinguaFlow.Web.Tests;

public sealed class CourseLoadingTests
{
    [Fact]
    public async Task CatalogAndManifest_DoNotEagerlyFetchUnits_AndUnitIsCached()
    {
        var (catalog, manifests, units, handler) = CreateServices();
        var entry = CourseFormatFixture.Entry();
        handler.Add("Courses/catalog.json", Json(new CourseCatalog { SchemaVersion = 1, Courses = [entry] }));
        handler.Add(entry.ManifestPath, Json(CourseFormatFixture.Manifest()));
        handler.Add("Courses/English/sample/Units/unit-01.json", Json(CourseFormatFixture.Unit()));

        var loadedCatalog = await catalog.GetCatalogAsync();
        var manifest = await manifests.GetManifestAsync(loadedCatalog.Courses.Single());
        Assert.Equal(2, handler.Requests.Count);
        Assert.DoesNotContain(handler.Requests, x => x.Contains("Units/"));
        await units.GetUnitAsync(entry, manifest, "unit-01");
        await units.GetUnitAsync(entry, manifest, "unit-01");
        Assert.Equal(1, handler.Requests.Count(x => x.Contains("Units/")));
    }

    [Fact]
    public async Task MissingUnitFile_ReportsFailure()
    {
        var (_, _, units, _) = CreateServices();
        var ex = await Assert.ThrowsAsync<CourseContentException>(() => units.GetUnitAsync(CourseFormatFixture.Entry(), CourseFormatFixture.Manifest(), "unit-01"));
        Assert.Contains("404", ex.Message);
    }

    [Fact]
    public async Task UnitCannotBeLoadedAcrossCourses()
    {
        var (_, _, units, _) = CreateServices();
        await Assert.ThrowsAsync<CourseContentException>(() => units.GetUnitAsync(CourseFormatFixture.Entry() with { CourseId = "different" }, CourseFormatFixture.Manifest(), "unit-01"));
    }

    [Fact]
    public async Task UnknownOptionalBlock_IsSkippedButRequiredBlockFails()
    {
        const string optional = """
            {"schemaVersion":1,"courseId":"sample","unitId":"unit-01","title":"Unit one","sections":[{"sectionId":"section-01","title":"Section","blocks":[{"type":"future-block","blockId":"future","optional":true},{"type":"explanation","blockId":"known","text":"Safe text"}]}]}
            """;
        var (_, _, units, handler) = CreateServices();
        handler.Add("Courses/English/sample/Units/unit-01.json", optional);
        var result = await units.GetUnitAsync(CourseFormatFixture.Entry(), CourseFormatFixture.Manifest(), "unit-01");
        Assert.Single(result.Content.Sections[0].Blocks);
        Assert.Single(result.Warnings);

        var (_, _, requiredUnits, requiredHandler) = CreateServices();
        requiredHandler.Add("Courses/English/sample/Units/unit-01.json", optional.Replace("\"optional\":true", "\"optional\":false"));
        await Assert.ThrowsAsync<CourseContentException>(() => requiredUnits.GetUnitAsync(CourseFormatFixture.Entry(), CourseFormatFixture.Manifest(), "unit-01"));
    }

    private static (CourseCatalogService Catalog, CourseManifestService Manifests, UnitContentService Units, FakeHandler Handler) CreateServices()
    {
        var handler = new FakeHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/project/") };
        var cache = new CourseContentCache();
        var paths = new CoursePathResolver();
        var validator = new CourseContentValidator();
        return (new(http, cache, paths, validator), new(http, cache, paths, validator), new(http, cache, paths, validator), handler);
    }

    private static string Json<T>(T value) => JsonSerializer.Serialize(value, CourseJson.Options);

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, string> _responses = [];
        public List<string> Requests { get; } = [];
        public void Add(string relativePath, string json) => _responses["https://example.test/project/" + relativePath] = json;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.AbsoluteUri;
            Requests.Add(url);
            return Task.FromResult(_responses.TryGetValue(url, out var body)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") }
                : new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}

internal static class CourseFormatFixture
{
    public static CourseCatalogEntry Entry() => new()
    {
        CourseId = "sample", LanguageCode = "en", LanguageDirectory = "English", Title = "Sample", Description = "Description", Level = "A2",
        Status = CourseStatus.Available, ManifestPath = "Courses/English/sample/manifest.json"
    };
    public static CourseManifest Manifest() => new()
    {
        SchemaVersion = 1, CourseId = "sample", ContentVersion = "1.0.0", ContentFingerprint = "sha256:" + new string('a', 64), LanguageCode = "en",
        Title = "Sample", Subtitle = "Subtitle", Description = "Description", LearningLevel = "A2",
        Units = [new UnitReference { UnitId = "unit-01", Title = "Unit one", Order = 1, Path = "Units/unit-01.json" }],
        Vocabulary = [], Assessments = [], AiPractice = [], Downloads = [], CompletionRules = new CompletionRules { Kind = "all-units" }
    };
    public static UnitContent Unit() => new()
    {
        SchemaVersion = 1, CourseId = "sample", UnitId = "unit-01", Title = "Unit one",
        Sections = [new UnitSection { SectionId = "section-01", Title = "Section", Blocks = [new ExplanationBlock { BlockId = "block-01", Text = "Safe text" }] }]
    };
}
