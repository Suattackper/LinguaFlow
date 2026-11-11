using System.Text.Json;
using LinguaFlow.Core.Courses;
using LinguaFlow.Web.Services;
using Xunit;

namespace LinguaFlow.Web.Tests;

public sealed class LessonJourneyTests
{
    [Fact]
    public void EveryPublishedUnitKeepsEverySectionInOriginalOrderAcrossJourney()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "LinguaFlow.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        var directory = Path.Combine(root.FullName, "src", "LinguaFlow.Web", "wwwroot", "Courses", "English", "english-life-office-it");
        var manifest = JsonSerializer.Deserialize<CourseManifest>(File.ReadAllText(Path.Combine(directory, "manifest.json")), CourseJson.Options)!;
        Assert.Equal(36, manifest.Units.Count);
        foreach (var reference in manifest.Units)
        {
            var unit = JsonSerializer.Deserialize<UnitContent>(File.ReadAllText(Path.Combine(directory, reference.Path.Replace('/', Path.DirectorySeparatorChar))), CourseJson.Options)!;
            var ordered = unit.Sections.OrderBy(s => s.Order).ToArray();
            Assert.NotEmpty(ordered);
            Assert.Equal(ordered.Length, ordered.Select(s => s.SectionId).Distinct(StringComparer.Ordinal).Count());
            var stages = LessonJourneyBuilder.Available(unit);
            Assert.Equal(stages.Count, stages.Distinct().Count());
            Assert.All(ordered, section => Assert.Contains(LessonJourneyBuilder.StageFor(section), stages));
            foreach (var stage in stages)
                Assert.Equal(ordered.First(s => LessonJourneyBuilder.StageFor(s) == stage).SectionId, LessonJourneyBuilder.First(unit, stage)?.SectionId);
            for (var index = 0; index < ordered.Length; index++)
            {
                Assert.Equal(index == 0 ? null : ordered[index - 1].SectionId, LessonJourneyBuilder.Next(unit, ordered[index].SectionId, -1)?.SectionId);
                Assert.Equal(index == ordered.Length - 1 ? null : ordered[index + 1].SectionId, LessonJourneyBuilder.Next(unit, ordered[index].SectionId, 1)?.SectionId);
            }
            Assert.Null(LessonJourneyBuilder.Next(unit, "missing-section", 1));
        }
    }
}
