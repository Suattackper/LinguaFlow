using System.Text.Json;
using LinguaFlow.Core.Analytics;
using LinguaFlow.Core.Assessment;
using LinguaFlow.Core.Courses;
using LinguaFlow.Core.Vocabulary;
using LinguaFlow.Web.Services;
using Xunit;

namespace LinguaFlow.Web.Tests;

public sealed class Phase04CourseUxTests
{
    private static (CourseManifest Manifest, string Directory) PublishedCourse()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "LinguaFlow.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        var directory = Path.Combine(root.FullName, "src", "LinguaFlow.Web", "wwwroot", "Courses", "English", "english-life-office-it");
        return (JsonSerializer.Deserialize<CourseManifest>(File.ReadAllText(Path.Combine(directory, "manifest.json")), CourseJson.Options)!, directory);
    }

    [Fact]
    public void ResumeUsesStoredPositionOrNextUnfinishedUnitAcrossAllPublishedUnits()
    {
        var (manifest, _) = PublishedCourse();
        Assert.Equal(36, manifest.Units.Count);
        var empty = LearningAnalyticsCalculator.Calculate(manifest, [], [], [], [], [], new LearningGoals(manifest.CourseId), DateTimeOffset.UtcNow, null);
        foreach (var unit in manifest.Units.OrderBy(x => x.Order))
        {
            var completed = manifest.Units.Where(x => x.Order < unit.Order).ToDictionary(x => x.UnitId, _ => SkillStatus.Completed);
            var analytics = empty with { UnitStatuses = completed, CurrentUnitId = unit.UnitId, CompletedUnits = completed.Count };
            var resume = CourseResumeResolver.Resolve(manifest, analytics, unit.UnitId, "g3");
            Assert.NotNull(resume);
            Assert.Equal(unit.UnitId, resume.Unit.UnitId);
            Assert.EndsWith($"/{unit.UnitId}#g3", resume.Href);
            Assert.True(resume.HasSavedPosition);
            var afterCompletion = CourseResumeResolver.Resolve(manifest, analytics with { UnitStatuses = new Dictionary<string, SkillStatus>(completed) { [unit.UnitId] = SkillStatus.Completed } }, unit.UnitId, "g3");
            Assert.NotEqual(unit.UnitId, afterCompletion?.Unit.UnitId);
        }
        var allDone = manifest.Units.ToDictionary(x => x.UnitId, _ => SkillStatus.Completed);
        Assert.Null(CourseResumeResolver.Resolve(manifest, empty with { UnitStatuses = allDone, CurrentUnitId = manifest.Units.Last().UnitId }, manifest.Units.Last().UnitId, "i"));
    }

    [Fact]
    public void DirectVocabularyQuerySelectsOnlyAnExistingUnitForEveryPublishedUnit()
    {
        var (manifest, directory) = PublishedCourse();
        var document = JsonSerializer.Deserialize<VocabularyDocument>(File.ReadAllText(Path.Combine(directory, manifest.Vocabulary[0].Path.Replace('/', Path.DirectorySeparatorChar))), CourseJson.Options)!;
        foreach (var unit in manifest.Units)
        {
            var selected = VocabularyUnitFilter.Resolve($"https://example.test/LinguaFlow/courses/{manifest.CourseId}/vocabulary?unit={unit.UnitId}", manifest);
            Assert.Equal(unit.UnitId, selected);
            Assert.Contains(document.Entries, word => word.UnitId == selected);
            Assert.All(document.Entries.Where(word => word.UnitId == selected), word => Assert.Equal(unit.UnitId, word.UnitId));
        }
        Assert.Equal("", VocabularyUnitFilter.Resolve("https://example.test/LinguaFlow/courses/example/vocabulary?unit=unit-99", manifest));
        Assert.Equal("", VocabularyUnitFilter.Resolve("https://example.test/LinguaFlow/courses/example/vocabulary", manifest));
    }
}
