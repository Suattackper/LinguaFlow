using System.Text.Json;
using LinguaFlow.Core.Courses;
using LinguaFlow.Core.Practice;
using LinguaFlow.Web.Services;
using Xunit;

namespace LinguaFlow.Web.Tests;

public sealed class Phase05PracticeCoverageTests
{
    [Fact]
    public void PublishedUnitsOfferOnlyAuthoredPracticeAndScopedFlashcards()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "LinguaFlow.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        var directory = Path.Combine(root.FullName, "src", "LinguaFlow.Web", "wwwroot", "Courses", "English", "english-life-office-it");
        T Read<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(directory, path.Replace('/', Path.DirectorySeparatorChar))), CourseJson.Options)!;
        var manifest = Read<CourseManifest>("manifest.json");
        var assessments = Read<AssessmentDocument>(manifest.Assessments[0].Path);
        var vocabulary = Read<VocabularyDocument>(manifest.Vocabulary[0].Path);
        var config = Read<PracticeConfiguration>(manifest.AiPractice[0].Path);
        Assert.Equal(36, manifest.Units.Count);
        foreach (var reference in manifest.Units)
        {
            var unit = Read<UnitContent>(reference.Path);
            Assert.Equal(reference.UnitId, VocabularyUnitFilter.Resolve($"https://example.test/LinguaFlow/courses/{manifest.CourseId}/flashcards?mode=new&unit={reference.UnitId}", manifest));
            Assert.Contains(vocabulary.Entries, word => word.UnitId == unit.UnitId);
            var tasks = unit.Sections.SelectMany(section => section.Blocks).Where(block => block is SpeakingBlock or WritingBlock or ListeningBlock).ToList();
            Assert.NotEmpty(tasks);
            var generated = 0;
            foreach (var task in tasks)
            {
                var skill = task switch { SpeakingBlock => "speaking", WritingBlock => "writing", _ => "listening" };
                foreach (var rubric in assessments.Rubrics.Where(r => r.UnitId == unit.UnitId && r.Skill.Contains(skill, StringComparison.OrdinalIgnoreCase)))
                {
                    try
                    {
                        var request = PracticePackageBuilder.Create(manifest, unit, task, rubric, config, vocabulary, "vi-VN", "standard", 15);
                        Assert.Equal(unit.UnitId, request.UnitId);
                        Assert.Equal(manifest.ContentFingerprint, request.CourseContentFingerprint);
                        Assert.Contains("linguaflow-practice-result", PracticePackageBuilder.BuildMarkdown(request));
                        generated++;
                        break;
                    }
                    catch (ArgumentException) { /* An authored rubric may not have a supported 100-point skill split. */ }
                }
            }
            Assert.True(generated > 0, $"No exportable authored task for {unit.UnitId}.");
        }
    }
}
