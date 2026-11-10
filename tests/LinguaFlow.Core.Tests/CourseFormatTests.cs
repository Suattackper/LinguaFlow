using System.Text;
using System.Text.Json;
using LinguaFlow.Core.Courses;
using Xunit;

namespace LinguaFlow.Core.Tests;

public sealed class CourseFormatTests
{
    private readonly CourseContentValidator _validator = new();

    [Fact]
    public void Catalog_ParsesStatusAndValidatesLanguageDirectory()
    {
        const string json = """
            {"schemaVersion":1,"courses":[{"courseId":"sample","languageCode":"en","languageDirectory":"English","title":"Sample","description":"Description","level":"A2","status":"available","manifestPath":"Courses/English/sample/manifest.json"}]}
            """;
        var catalog = JsonSerializer.Deserialize<CourseCatalog>(json, CourseJson.Options);
        Assert.NotNull(catalog);
        Assert.Equal(CourseStatus.Available, catalog.Courses[0].Status);
        Assert.Empty(_validator.ValidateCatalog(catalog));
    }

    [Fact]
    public void Manifest_ParsesAndValidatesReferences()
    {
        var manifest = JsonSerializer.Deserialize<CourseManifest>(JsonSerializer.Serialize(Manifest(), CourseJson.Options), CourseJson.Options);
        Assert.NotNull(manifest);
        Assert.Empty(_validator.ValidateManifest(manifest, Entry()));
    }

    [Fact]
    public void DuplicateCourseAndUnitIds_AreRejected()
    {
        var entry = Entry();
        Assert.Contains(_validator.ValidateCatalog(new CourseCatalog { SchemaVersion = 1, Courses = [entry, entry] }), e => e.Contains("duplicate courseId"));
        var unit = Manifest().Units[0];
        Assert.Contains(_validator.ValidateManifest(Manifest() with { Units = [unit, unit with { Order = 2 }] }, entry), e => e.Contains("duplicate unitId"));
    }

    [Theory]
    [InlineData("../other/secret.json")]
    [InlineData("/Courses/English/sample/Units/u.json")]
    [InlineData("https://example.com/file.json")]
    [InlineData("Units/%2e%2e/file.json")]
    [InlineData("Units\\unit-01.json")]
    public void PathResolver_RejectsTraversalAndExternalPaths(string path)
    {
        var resolver = new CoursePathResolver();
        Assert.Throws<CourseContentException>(() => resolver.ResolveContent(new Uri("https://example.com/project/Courses/English/sample/manifest.json"), path));
    }

    [Fact]
    public void PathResolver_PreservesGitHubPagesSubpath()
    {
        var resolver = new CoursePathResolver();
        var manifestUrl = resolver.ResolveManifest(new Uri("https://example.com/project/"), Entry());
        Assert.Equal("https://example.com/project/Courses/English/sample/Units/unit-01.json", resolver.ResolveContent(manifestUrl, "Units/unit-01.json").AbsoluteUri);
    }

    [Fact]
    public void InvalidReferencesAndLanguageMetadata_AreRejected()
    {
        Assert.Contains(_validator.ValidateCatalog(new CourseCatalog { SchemaVersion = 1, Courses = [Entry() with { LanguageDirectory = "Japan" }] }), e => e.Contains("Inconsistent language"));
        Assert.Contains(_validator.ValidateManifest(Manifest() with { Units = [Manifest().Units[0] with { Path = "../unit.json" }] }, Entry()), e => e.Contains("Invalid unit reference"));
        Assert.Contains(_validator.ValidateManifest(Manifest() with { CourseId = "another" }, Entry()), e => e.Contains("does not match"));
    }

    [Fact]
    public void UnsupportedSchemas_AreRejected()
    {
        Assert.Contains(_validator.ValidateCatalog(new CourseCatalog { SchemaVersion = 2, Courses = [Entry()] }), e => e.Contains("Unsupported"));
        Assert.Contains(_validator.ValidateManifest(Manifest() with { SchemaVersion = 2 }, Entry()), e => e.Contains("Unsupported"));
        Assert.Contains(_validator.ValidateUnit(Unit() with { SchemaVersion = 2 }, Manifest(), "unit-01"), e => e.Contains("Unsupported"));
    }

    [Fact]
    public void MissingRequiredProperty_AndDuplicateBlockIds_AreRejected()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CourseCatalog>("{\"schemaVersion\":1,\"courses\":[{\"courseId\":\"sample\"}]}", CourseJson.Options));
        var block = new ExplanationBlock { BlockId = "block-01", Text = "Safe" };
        var unit = Unit() with { Sections = [new UnitSection { SectionId = "section-01", Title = "Section", Blocks = [block, block] }] };
        Assert.Contains(_validator.ValidateUnit(unit, Manifest(), "unit-01"), e => e.Contains("duplicate blockId"));
    }

    [Fact]
    public void Fingerprint_IsDeterministicAndSensitiveToBytes()
    {
        var first = ContentFingerprint.Compute([("Units/a.json", Encoding.UTF8.GetBytes("a")), ("Units/b.json", Encoding.UTF8.GetBytes("b"))]);
        var reversed = ContentFingerprint.Compute([("Units/b.json", Encoding.UTF8.GetBytes("b")), ("Units/a.json", Encoding.UTF8.GetBytes("a"))]);
        var changed = ContentFingerprint.Compute([("Units/a.json", Encoding.UTF8.GetBytes("A")), ("Units/b.json", Encoding.UTF8.GetBytes("b"))]);
        Assert.Equal(first, reversed);
        Assert.NotEqual(first, changed);
    }

    internal static CourseCatalogEntry Entry() => new()
    {
        CourseId = "sample", LanguageCode = "en", LanguageDirectory = "English", Title = "Sample", Description = "Description", Level = "A2",
        Status = CourseStatus.Available, ManifestPath = "Courses/English/sample/manifest.json"
    };

    internal static CourseManifest Manifest() => new()
    {
        SchemaVersion = 1, CourseId = "sample", ContentVersion = "1.0.0", ContentFingerprint = "sha256:" + new string('a', 64), LanguageCode = "en",
        Title = "Sample", Subtitle = "Subtitle", Description = "Description", LearningLevel = "A2",
        Units = [new UnitReference { UnitId = "unit-01", Title = "Unit one", Order = 1, Path = "Units/unit-01.json" }],
        Vocabulary = [], Assessments = [], AiPractice = [], Downloads = [], CompletionRules = new CompletionRules { Kind = "all-units" }
    };

    internal static UnitContent Unit() => new()
    {
        SchemaVersion = 1, CourseId = "sample", UnitId = "unit-01", Title = "Unit one",
        Sections = [new UnitSection { SectionId = "section-01", Title = "Section", Blocks = [new ExplanationBlock { BlockId = "block-01", Text = "Safe" }] }]
    };
}
