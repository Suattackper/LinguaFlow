using System.Text.Json;
using LinguaFlow.Core.Assessment;
using LinguaFlow.Core.Courses;
using LinguaFlow.Web.Services;
using Xunit;

namespace LinguaFlow.Web.Tests;

public sealed class AssessmentPresentationTests
{
    private static string CourseDirectory
    {
        get
        {
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root is not null && !File.Exists(Path.Combine(root.FullName, "LinguaFlow.slnx"))) root = root.Parent;
            Assert.NotNull(root);
            return Path.Combine(root.FullName, "src", "LinguaFlow.Web", "wwwroot", "Courses", "English", "english-life-office-it");
        }
    }

    [Fact]
    public void EveryPublishedUnitHasMetadataDrivenChecklistWithValidLinksAndNoInventedPass()
    {
        var directory = CourseDirectory;
        var manifest = JsonSerializer.Deserialize<CourseManifest>(File.ReadAllText(Path.Combine(directory, "manifest.json")), CourseJson.Options)!;
        var document = JsonSerializer.Deserialize<AssessmentDocument>(File.ReadAllText(Path.Combine(directory, manifest.Assessments[0].Path.Replace('/', Path.DirectorySeparatorChar))), CourseJson.Options)!;
        Assert.Equal(36, manifest.Units.Count);
        foreach (var reference in manifest.Units)
        {
            var unit = JsonSerializer.Deserialize<UnitContent>(File.ReadAllText(Path.Combine(directory, reference.Path.Replace('/', Path.DirectorySeparatorChar))), CourseJson.Options)!;
            var empty = new UnitCompletionService().Evaluate(unit.UnitId, document.Thresholds, []);
            var checklist = AssessmentChecklistBuilder.Build(unit, document.Thresholds, empty);
            Assert.NotEmpty(checklist);
            Assert.DoesNotContain(checklist, x => x.Status == SkillStatus.Passed);
            Assert.All(checklist.Where(x => x.SectionId is not null), x => Assert.Contains(unit.Sections, s => s.SectionId == x.SectionId));
            Assert.Contains(checklist, x => x.Skill == "grammar" && x.SectionId is not null);
            Assert.Contains(checklist, x => x.Skill == "vocabulary" && x.SectionId is not null);
            Assert.Contains(checklist, x => x.Skill == "reading" && x.SectionId is not null);
        }
    }

    [Fact]
    public void EveryPublishedResponseBlockHasAStableDefinitionAndAuthoredAnswerMapping()
    {
        var directory = CourseDirectory;
        var manifest = JsonSerializer.Deserialize<CourseManifest>(File.ReadAllText(Path.Combine(directory, "manifest.json")), CourseJson.Options)!;
        var document = JsonSerializer.Deserialize<AssessmentDocument>(File.ReadAllText(Path.Combine(directory, manifest.Assessments[0].Path.Replace('/', Path.DirectorySeparatorChar))), CourseJson.Options)!;
        var foundKinds = new HashSet<ExerciseKind>();
        foreach (var reference in manifest.Units)
        {
            var unit = JsonSerializer.Deserialize<UnitContent>(File.ReadAllText(Path.Combine(directory, reference.Path.Replace('/', Path.DirectorySeparatorChar))), CourseJson.Options)!;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var section in unit.Sections)
            foreach (var block in section.Blocks)
            {
                var id = block switch
                {
                    QuestionBlock q => q.ExerciseId,
                    WritingBlock w => w.ExerciseId,
                    SpeakingBlock s => s.ExerciseId,
                    ListeningBlock l => l.ExerciseId,
                    _ => null
                };
                if (id is null) continue;
                Assert.True(ids.Add(id), $"Duplicate response ID: {unit.UnitId}/{id}");
                var key = document.Answers.FirstOrDefault(a => a.UnitId == unit.UnitId && a.ExerciseId == id);
                var definition = ExerciseDefinitionFactory.Create(manifest.CourseId, unit.UnitId, section, block, key);
                Assert.Equal(id, definition.ExerciseId);
                Assert.Equal(unit.UnitId, definition.UnitId);
                Assert.Equal(manifest.CourseId, definition.CourseId);
                Assert.False(string.IsNullOrWhiteSpace(definition.Prompt));
                Assert.False(definition.AutomaticGrading && key is null);
                foundKinds.Add(definition.Kind);
            }
            Assert.NotEmpty(ids);
        }
        Assert.Contains(ExerciseKind.SingleChoice, foundKinds);
        Assert.Contains(ExerciseKind.FillBlank, foundKinds);
        Assert.Contains(ExerciseKind.Writing, foundKinds);
        Assert.Contains(ExerciseKind.Speaking, foundKinds);
        Assert.Contains(ExerciseKind.Listening, foundKinds);
    }

    [Fact]
    public void Unit01ChecklistUsesSubmittedScoreWithoutMarkingUnfinishedUnitPassed()
    {
        var directory = CourseDirectory;
        var unit = JsonSerializer.Deserialize<UnitContent>(File.ReadAllText(Path.Combine(directory, "Units", "unit-01.json")), CourseJson.Options)!;
        var document = JsonSerializer.Deserialize<AssessmentDocument>(File.ReadAllText(Path.Combine(directory, "Assessments", "assessments.json")), CourseJson.Options)!;
        var empty = new UnitCompletionService().Evaluate(unit.UnitId, document.Thresholds, []);
        Assert.Equal(SkillStatus.NotStarted, empty.Status);
        var answer = document.Answers.Single(a => a.ExerciseId == "unit-01-c1-06");
        var section = unit.Sections.Single(s => s.Blocks.OfType<QuestionBlock>().Any(q => q.ExerciseId == answer.ExerciseId));
        var block = section.Blocks.OfType<QuestionBlock>().Single(q => q.ExerciseId == answer.ExerciseId);
        var definition = ExerciseDefinitionFactory.Create("english-life-office-it", unit.UnitId, section, block, answer);
        var feedback = new ExerciseGradingService().Grade(definition, new ExerciseResponse { ExerciseId = answer.ExerciseId, Answers = ["Does", "join"] });
        var attempt = new ExerciseAttempt { AttemptId = "unit01-test", CourseId = definition.CourseId, UnitId = unit.UnitId,
            ExerciseId = definition.ExerciseId, SubmittedAt = DateTimeOffset.UtcNow, Mode = ExerciseMode.Practice,
            Answers = ["Does", "join"], Feedback = feedback };
        var partial = new UnitCompletionService().Evaluate(unit.UnitId, document.Thresholds, [attempt]);
        var checklist = AssessmentChecklistBuilder.Build(unit, document.Thresholds, partial);
        Assert.Equal(GradingStatus.Correct, feedback.Status);
        Assert.NotEqual(SkillStatus.Completed, partial.Status);
        Assert.Contains(partial.Skills, skill => skill.Skill == "grammar" && skill.Earned == feedback.EarnedScore && skill.Maximum == feedback.MaximumScore);
        Assert.DoesNotContain(checklist, item => item.Skill == "grammar" && item.Status == SkillStatus.Passed);
    }

    [Theory]
    [InlineData(GradingStatus.Correct, ExerciseKind.SingleChoice, ExerciseUiState.Correct)]
    [InlineData(GradingStatus.Incorrect, ExerciseKind.SingleChoice, ExerciseUiState.Incorrect)]
    [InlineData(GradingStatus.Partial, ExerciseKind.MultipleSelect, ExerciseUiState.Partial)]
    [InlineData(GradingStatus.NeedsReview, ExerciseKind.FillBlank, ExerciseUiState.SelfReviewNeeded)]
    [InlineData(GradingStatus.NeedsReview, ExerciseKind.Writing, ExerciseUiState.PendingExternalEvaluation)]
    [InlineData(GradingStatus.NeedsReview, ExerciseKind.Speaking, ExerciseUiState.PendingExternalEvaluation)]
    [InlineData(GradingStatus.InsufficientEvidence, ExerciseKind.Listening, ExerciseUiState.InsufficientEvidence)]
    public void FeedbackStateReflectsStoredResultWithoutChangingIt(GradingStatus status, ExerciseKind kind, ExerciseUiState expected)
    {
        var definition = new ExerciseDefinition { CourseId = "sample", UnitId = "unit-02", ExerciseId = "q", Kind = kind, Prompt = "Prompt", Skill = "grammar" };
        var feedback = new ExerciseFeedback { Status = status, EarnedScore = status == GradingStatus.Correct ? 1 : 0, MaximumScore = 1, Message = "message" };
        var attempt = new ExerciseAttempt { AttemptId = "old", CourseId = "sample", UnitId = "unit-02", ExerciseId = "q", SubmittedAt = DateTimeOffset.UtcNow, Mode = ExerciseMode.Practice, Answers = ["answer"], Feedback = feedback };
        Assert.Equal(expected, ExerciseStatePresenter.Resolve(definition, "answer", attempt, ExerciseMode.Practice, false));
        Assert.Equal(status, attempt.Feedback.Status);
    }

    [Fact]
    public void EditingLockedAndAuthoredAlternativeAreDistinct()
    {
        var definition = new ExerciseDefinition { CourseId = "sample", UnitId = "unit-02", ExerciseId = "q", Kind = ExerciseKind.FillBlank, Prompt = "Prompt", Skill = "grammar", ValidAnswers = ["works", "is working"] };
        Assert.Equal(ExerciseUiState.NotStarted, ExerciseStatePresenter.Resolve(definition, "", null, ExerciseMode.Practice, false));
        Assert.Equal(ExerciseUiState.Editing, ExerciseStatePresenter.Resolve(definition, "draft", null, ExerciseMode.Practice, false));
        Assert.Equal(ExerciseUiState.Locked, ExerciseStatePresenter.Resolve(definition, "draft", null, ExerciseMode.Test, false));
        var attempt = new ExerciseAttempt { AttemptId = "old", CourseId = "sample", UnitId = "unit-02", ExerciseId = "q", SubmittedAt = DateTimeOffset.UtcNow, Mode = ExerciseMode.Practice, Answers = ["is working"], Feedback = new ExerciseFeedback { Status = GradingStatus.Correct, EarnedScore = 1, MaximumScore = 1, Message = "FeedbackCorrect" } };
        Assert.Equal(ExerciseUiState.AcceptedVariant, ExerciseStatePresenter.Resolve(definition, "is working", attempt, ExerciseMode.Practice, false));
    }
}
