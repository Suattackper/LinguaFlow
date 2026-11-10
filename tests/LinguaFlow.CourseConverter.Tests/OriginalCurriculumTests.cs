using System.Text;
using System.Text.Json;
using LinguaFlow.Core.Courses;
using LinguaFlow.CourseConverter;
using LinguaFlow.Core.Assessment;
using Xunit;

namespace LinguaFlow.CourseConverter.Tests;

public sealed class OriginalCurriculumTests
{
    private static readonly string Root = FindRepositoryRoot();
    private static readonly string SourceRoot = Path.Combine(Root, "CourseSources", "English");

    [Fact]
    public async Task PublishedAnswerPolicyGradesOnlyAuthoredUnambiguousChoices()
    {
        var plan = await ConversionPipeline.BuildAsync(SourceRoot);
        Assert.Equal(36, plan.Assessments.Answers.Count(a => a.AssessmentMode == "auto-choice"));
        var unit = plan.Units[0];
        var section = unit.Sections.Single(s => s.SectionId == "c2");
        var question = section.Blocks.OfType<QuestionBlock>().First();
        var key = plan.Assessments.Answers.Single(a => a.ExerciseId == question.ExerciseId);
        var definition = ExerciseDefinitionFactory.Create(plan.Manifest.CourseId, unit.UnitId, section, question, key);
        var feedback = new ExerciseGradingService().Grade(definition, new ExerciseResponse { ExerciseId = question.ExerciseId, Answers = ["B. I write a report every Friday."] });
        Assert.Equal(GradingStatus.Correct, feedback.Status);
        var fillSection = unit.Sections.Single(s => s.SectionId == "c1");
        var fill = fillSection.Blocks.OfType<QuestionBlock>().First();
        var fillKey = plan.Assessments.Answers.Single(a => a.ExerciseId == fill.ExerciseId);
        Assert.False(ExerciseDefinitionFactory.Create(plan.Manifest.CourseId, unit.UnitId, fillSection, fill, fillKey).AutomaticGrading);
    }

    [Fact]
    public async Task DiscoversOriginalThirtySixUnitsInOrderWithOriginalTitles()
    {
        var sources = SourceDiscovery.Discover(SourceRoot);
        var plan = await ConversionPipeline.BuildAsync(SourceRoot);
        Assert.Equal(36, sources.Lessons.Count);
        Assert.Equal(36, sources.AnswerKeys.Count);
        Assert.Equal(36, plan.Units.Count);
        Assert.Equal(Enumerable.Range(1, 36).Select(x => $"unit-{x:00}"), plan.Units.Select(x => x.UnitId));
        Assert.Equal("A Typical Day at Work", plan.Units[0].Title);
        Assert.Equal("B1 Workplace Project", plan.Units[17].Title);
        Assert.Equal("B2 Capstone and Evaluation", plan.Units[35].Title);
        Assert.Equal("checkpoint", plan.Units[11].Kind);
        Assert.Equal("capstone", plan.Units[35].Kind);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(18)]
    [InlineData(36)]
    public async Task RepresentativeUnitsPreserveEverySourceSection(int number)
    {
        var plan = await ConversionPipeline.BuildAsync(SourceRoot);
        var source = await File.ReadAllTextAsync(Path.Combine(SourceRoot, "english-life-office-it", "LESSONS", $"Unit_{number:00}.md"));
        var original = MarkdownStructure.Split(source);
        var converted = plan.Units[number - 1];
        Assert.Equal(original.Count, converted.Sections.Count);
        Assert.Equal(original.Select(x => x.Raw), converted.Sections.Select(x => x.SourceMarkdown));
        Assert.All(converted.Sections, section => Assert.Equal($"LESSONS/Unit_{number:00}.md", section.Source!.File));
        Assert.NotEmpty(converted.Objectives);
        Assert.Equal([1, 3, 7, 14], converted.ReviewScheduleDays);
    }

    [Fact]
    public async Task AnswerMappingsAreOriginalAndNoExerciseIdsRepeat()
    {
        var plan = await ConversionPipeline.BuildAsync(SourceRoot);
        var ids = plan.Units.SelectMany(x => x.Sections).SelectMany(x => x.Blocks)
            .OfType<QuestionBlock>().Select(x => x.ExerciseId).ToArray();
        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(ids.Length, plan.Assessments.Answers.Count);
        Assert.Equal("B", plan.Assessments.Answers.Single(x => x.ExerciseId == "unit-01-c2-01").Answer);
        Assert.Equal("auto-choice", plan.Assessments.Answers.Single(x => x.ExerciseId == "unit-01-c2-01").AssessmentMode);
        Assert.Equal("manual-review", plan.Assessments.Answers.Single(x => x.ExerciseId == "unit-18-d1-01").AssessmentMode);
        Assert.Equal("who led / that led", plan.Assessments.Answers.Single(x => x.ExerciseId == "unit-18-c1-03").Answer);
        Assert.Equal("would test / could test", plan.Assessments.Answers.Single(x => x.ExerciseId == "unit-36-c1-03").Answer);
        Assert.All(plan.Assessments.Answers, x => Assert.StartsWith("ANSWER_KEYS/", x.Source.File));
    }

    [Fact]
    public async Task RepeatedConversionHasStableIdsBytesAndFingerprint()
    {
        var first = await ConversionPipeline.BuildAsync(SourceRoot);
        var second = await ConversionPipeline.BuildAsync(SourceRoot);
        Assert.Equal(first.Manifest.ContentFingerprint, second.Manifest.ContentFingerprint);
        Assert.Equal(first.Files.Keys, second.Files.Keys);
        foreach (var key in first.Files.Keys) Assert.Equal(first.Files[key], second.Files[key]);
        Assert.Equal(first.Units.SelectMany(x => x.Sections).Select(x => x.SectionId), second.Units.SelectMany(x => x.Sections).Select(x => x.SectionId));
    }

    [Fact]
    public async Task GeneratedDocumentsValidateAndContainNoPrivateSourcePaths()
    {
        var plan = await ConversionPipeline.BuildAsync(SourceRoot);
        Assert.Empty(ConversionPipeline.Validate(plan.Catalog, plan.Manifest, plan.Units, plan.Vocabulary, plan.Assessments, plan.Practice));
        Assert.Equal(720, plan.Vocabulary.Entries.Count);
        Assert.Equal(36, plan.Vocabulary.Entries.Select(x => x.UnitId).Distinct().Count());
        Assert.Single(plan.Manifest.Vocabulary);
        Assert.Single(plan.Manifest.Assessments);
        Assert.Single(plan.Manifest.AiPractice);
        foreach (var (relative, bytes) in plan.Files)
        {
            Assert.DoesNotContain("E:\\", Encoding.UTF8.GetString(bytes));
            using var _ = JsonDocument.Parse(bytes);
            Assert.True(CoursePathResolver.IsSafeRelativePath(relative));
        }
    }

    [Fact]
    public async Task PublishedCoursePassesReferenceAndFingerprintValidation()
    {
        await ConverterCli.ValidatePublishedAsync(Path.Combine(Root, "src", "LinguaFlow.Web", "wwwroot", "Courses"));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "LinguaFlow.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
