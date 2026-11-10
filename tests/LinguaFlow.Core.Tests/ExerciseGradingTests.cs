using LinguaFlow.Core.Assessment;
using LinguaFlow.Core.Courses;
using Xunit;

namespace LinguaFlow.Core.Tests;

public sealed class ExerciseGradingTests
{
    private readonly ExerciseGradingService _grading = new();
    private static ExerciseDefinition Definition(ExerciseKind kind, string[] answers, bool automatic = true, bool partial = false) => new()
    {
        CourseId = "sample", UnitId = "unit-01", ExerciseId = "unit-01-c2-01", Kind = kind,
        Prompt = "Authored prompt", Skill = "grammar", ValidAnswers = answers,
        Options = ["A. wrong", "B. right", "C. other"], AutomaticGrading = automatic,
        AllowPartialCredit = partial, Explanation = "Original explanation", MaximumScore = 2
    };
    private static ExerciseResponse Response(params string[] answers) => new() { ExerciseId = "unit-01-c2-01", Answers = answers };

    [Fact]
    public void SingleChoice_UsesActualKeyAndReturnsExplanation()
    {
        var d = Definition(ExerciseKind.SingleChoice, ["B"]);
        var correct = _grading.Grade(d, Response("B. right"));
        var wrong = _grading.Grade(d, Response("A. wrong"));
        Assert.Equal(GradingStatus.Correct, correct.Status);
        Assert.Equal(2, correct.EarnedScore);
        Assert.Equal("Original explanation", wrong.Explanation);
        Assert.Equal(GradingStatus.Incorrect, wrong.Status);
    }
    [Fact]
    public void MultipleSelect_RequiresTheExactSet()
    {
        var d = Definition(ExerciseKind.MultipleSelect, ["A", "C"]);
        Assert.Equal(GradingStatus.Correct, _grading.Grade(d, Response("C. other", "A. wrong")).Status);
        Assert.Equal(GradingStatus.Incorrect, _grading.Grade(d, Response("A. wrong")).Status);
    }
    [Fact]
    public void AlternativesAndUnicodeWhitespace_AreHandledWithoutAggressiveGrammarNormalization()
    {
        var d = Definition(ExerciseKind.FillBlank, ["does not work", "doesn't work"]);
        Assert.Equal(GradingStatus.Correct, _grading.Grade(d, Response("  DOES   NOT WORK  ")).Status);
        Assert.Equal(GradingStatus.Correct, _grading.Grade(d, Response("doesn't work")).Status);
        Assert.Equal(GradingStatus.Incorrect, _grading.Grade(d, Response("does work")).Status);
    }
    [Fact]
    public void EmptyAndUnknownChoice_AreRejected()
    {
        var d = Definition(ExerciseKind.SingleChoice, ["B"]);
        Assert.Throws<ArgumentException>(() => _grading.Grade(d, Response("")));
        Assert.Throws<ArgumentException>(() => _grading.Grade(d, Response("D")));
    }
    [Fact]
    public void PartialCredit_RequiresExplicitPermission()
    {
        var d = Definition(ExerciseKind.MultipleBlanks, ["is", "working"], partial: true);
        Assert.Equal(1, _grading.Grade(d, Response("is", "work")).EarnedScore);
        Assert.Equal(0, _grading.Grade(d with { AllowPartialCredit = false }, Response("is", "work")).EarnedScore);
    }
    [Fact]
    public void WritingAndListening_DoNotReceiveFabricatedScores()
    {
        Assert.Equal(GradingStatus.NeedsReview, _grading.Grade(Definition(ExerciseKind.Writing, [], false), Response("An authored draft")).Status);
        Assert.Equal(GradingStatus.InsufficientEvidence, _grading.Grade(Definition(ExerciseKind.Listening, [], false), Response("A text summary")).Status);
    }
    [Fact]
    public void SourceManualReviewFlag_DisablesAutomaticScoring()
    {
        var section = new UnitSection { SectionId = "c1", Title = "Fill", Blocks = [] };
        var block = new QuestionBlock { BlockId = "block", ExerciseId = "unit-01-c1-01", Prompt = "He ____ every day.", ResponseKind = "fill-blank" };
        var key = new AnswerRecord { ExerciseId = block.ExerciseId, UnitId = "unit-01", Answer = "works", Explanation = "Present simple", AssessmentMode = "manual-review", Source = new SourceProvenance { File = "ANSWER_KEYS/unit.md", OriginalUnit = "01", OriginalSection = "C1" } };
        var definition = ExerciseDefinitionFactory.Create("sample", "unit-01", section, block, key);
        Assert.False(definition.AutomaticGrading);
        Assert.Equal(GradingStatus.NeedsReview, _grading.Grade(definition, new ExerciseResponse { ExerciseId = block.ExerciseId, Answers = ["works"] }).Status);
    }
    [Fact]
    public void Completion_RequiresAuthoredThresholdAndOpenEvidence()
    {
        var rules = new List<ThresholdRule> { new() { UnitId = "unit-01", Skill = "grammar", Text = "≥1/1", MinimumScore = 1, TotalScore = 1 }, new() { UnitId = "unit-01", Skill = "progression", Text = "Writing completed" } };
        var attempt = new ExerciseAttempt { AttemptId = "attempt-one", CourseId = "sample", UnitId = "unit-01", ExerciseId = "unit-01-c2-01", SubmittedAt = DateTimeOffset.UtcNow, Mode = ExerciseMode.Practice, Answers = ["B"], Feedback = new ExerciseFeedback { Status = GradingStatus.Correct, EarnedScore = 1, MaximumScore = 1, Message = "Correct" } };
        var result = new UnitCompletionService().Evaluate("unit-01", rules, [attempt]);
        Assert.Equal(SkillStatus.Passed, result.Skills[0].Status);
        Assert.NotEqual(SkillStatus.Completed, result.Status);
        Assert.Contains(result.RemainingRequirements, x => x.Contains("Writing"));
    }
}
