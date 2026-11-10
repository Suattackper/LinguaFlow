using System.Text.Json;
using LinguaFlow.Core.Courses;
using LinguaFlow.Core.Practice;
using LinguaFlow.Core.Progress;
using LinguaFlow.Core.Assessment;
using Xunit;

namespace LinguaFlow.Core.Tests;

public sealed class PracticeResultTests
{
    private static string Root => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/LinguaFlow.Web/wwwroot/Courses/English/english-life-office-it"));
    private static T Read<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(Root, path)), CourseJson.Options)!;
    private static (PracticeSession Session, CourseManifest Manifest, PracticeResult Result) Sample()
    {
        var manifest = Read<CourseManifest>("manifest.json");
        var unit = Read<UnitContent>("Units/unit-01.json");
        var assessments = Read<AssessmentDocument>("Assessments/assessments.json");
        var vocabulary = Read<VocabularyDocument>("Vocabulary/vocabulary.json");
        var config = Read<PracticeConfiguration>("Prompts/practice-config.json");
        var task = unit.Sections.SelectMany(s => s.Blocks).OfType<SpeakingBlock>().First();
        var rubric = assessments.Rubrics.Single(r => r.UnitId == unit.UnitId && r.Skill.Contains("speaking"));
        var request = PracticePackageBuilder.Create(manifest, unit, task, rubric, config, vocabulary, "vi-VN", "standard", 15);
        var session = new PracticeSession { Request = request, PackageMarkdown = PracticePackageBuilder.BuildMarkdown(request), Status = PracticeSessionStatus.Exported, UpdatedAt = DateTimeOffset.UtcNow };
        var result = new PracticeResult
        {
            Format = "linguaflow-practice-result", SchemaVersion = 1, RequestId = request.RequestId,
            CourseId = request.CourseId, CourseContentVersion = request.CourseContentVersion, UnitId = request.UnitId,
            TaskId = request.TaskId, Skill = request.Skill, RubricId = request.RubricId, RubricVersion = request.RubricVersion,
            CompletedAt = DateTimeOffset.UtcNow, Evaluator = "external-ai:ChatGPT", Evidence = new("text-only", "Text responses only"),
            CriterionAssessments = request.AssessmentCriteria.Select(c => new PracticeCriterionAssessment(c.CriterionId, c.MaxPoints, c.MaxPoints, "sufficient", "Observed learner response")).ToList(),
            Strengths = ["Clear response"], Mistakes = [], VocabularySuggestions = [], ImprovementSuggestions = [], Summary = "Completed."
        };
        return (session, manifest, result);
    }
    private static PracticeValidation Check(PracticeSession s, CourseManifest m, PracticeResult r, ThresholdRule? rule = null) => new PracticeResultValidator().Validate(JsonSerializer.Serialize(r, new JsonSerializerOptions(JsonSerializerDefaults.Web)), s, m, rule);

    [Fact]
    public void MatchingResultScoresButDoesNotInventPassThreshold()
    {
        var (s, m, r) = Sample();
        var validated = Check(s, m, r);
        Assert.True(validated.Valid, string.Join(" ", validated.Errors));
        Assert.Equal(100, validated.Evaluation!.EarnedPoints);
        Assert.Equal(PracticeVerdict.NotAssessed, validated.Evaluation.Verdict);
        Assert.Contains(validated.Evaluation.Warnings, w => w.Contains("NOT ASSESSED"));
        var fenced = "```json\n" + JsonSerializer.Serialize(r, new JsonSerializerOptions(JsonSerializerDefaults.Web)) + "\n```";
        Assert.True(new PracticeResultValidator().Validate(fenced, s, m).Valid);
        Assert.Equal(r.RequestId, PracticeResultValidator.ReadRequestId(fenced));
        Assert.Equal(PracticeResultValidator.ResultId(r), PracticeResultValidator.ResultId(r));
    }
    [Fact]
    public void OnlyAuthoredSkillThresholdCanProducePassOrFailure()
    {
        var (s, m, r) = Sample();
        var rule = new ThresholdRule { UnitId = r.UnitId, Skill = r.Skill, Text = "authored speaking threshold 80%", MinimumPercent = 80 };
        Assert.Equal(PracticeVerdict.Passed, Check(s, m, r, rule).Evaluation!.Verdict);
        var low = r with { CriterionAssessments = r.CriterionAssessments.Select(c => c with { AwardedPoints = 0 }).ToList() };
        Assert.Equal(PracticeVerdict.NeedsImprovement, Check(s, m, low, rule).Evaluation!.Verdict);
        Assert.Equal(PracticeVerdict.NotAssessed, Check(s, m, r, rule with { Skill = "grammar" }).Evaluation!.Verdict);
    }
    [Fact]
    public void MismatchesAndImpossibleScoresAreRejected()
    {
        var (s, m, r) = Sample();
        Assert.Contains(Check(s, m, r with { RequestId = "wrong" }).Errors, e => e.Contains("Request ID"));
        Assert.Contains(Check(s, m, r with { CourseId = "other" }).Errors, e => e.Contains("Course ID"));
        Assert.Contains(Check(s, m, r with { UnitId = "unit-02" }).Errors, e => e.Contains("Unit ID"));
        Assert.Contains(Check(s, m, r with { RubricId = "wrong" }).Errors, e => e.Contains("Rubric"));
        Assert.Contains(Check(s, m, r with { CriterionAssessments = r.CriterionAssessments.Skip(1).ToList() }).Errors, e => e.Contains("count"));
        Assert.Contains(Check(s, m, r with { CriterionAssessments = r.CriterionAssessments.Select((c, i) => i == 0 ? c with { AwardedPoints = 999 } : c).ToList() }).Errors, e => e.Contains("Invalid points"));
        Assert.Contains(Check(s, m, r with { CriterionAssessments = r.CriterionAssessments.Select(c => c with { CriterionId = "fake" }).ToList() }).Errors, e => e.Contains("Unknown criterion"));
    }
    [Fact]
    public void TextOnlyCannotScoreAudioAndInsufficientEvidenceStaysPending()
    {
        var (s, m, r) = Sample();
        var criteria = s.Request.AssessmentCriteria.ToList();
        criteria[0] = criteria[0] with { Title = "Pronunciation", EvidenceRequirement = "audio observed" };
        var audioSession = s with { Request = s.Request with { AssessmentCriteria = criteria } };
        Assert.Contains(Check(audioSession, m, r).Errors, e => e.Contains("Audio evidence"));
        var partial = r with { CriterionAssessments = r.CriterionAssessments.Select((c, i) => i == 0 ? c with { AwardedPoints = null, EvidenceLevel = "insufficient" } : c).ToList() };
        Assert.Equal(PracticeVerdict.InsufficientEvidence, Check(s, m, partial).Evaluation!.Verdict);
        Assert.False(Check(s, m, partial).Evaluation!.EvidenceComplete);
    }
    [Fact]
    public void InvalidJsonAndUntrustedMarkupRemainPlainData()
    {
        var (s, m, r) = Sample();
        Assert.False(new PracticeResultValidator().Validate("```json\n{bad}\n```", s, m).Valid);
        Assert.Null(PracticeResultValidator.ReadRequestId(new string('x', PracticeResultValidator.MaxBytes + 1)));
        var hostile = r with { Summary = "<script>alert(1)</script>" };
        Assert.True(Check(s, m, hostile).Valid);
        Assert.Equal("<script>alert(1)</script>", Check(s, m, hostile).Result!.Summary);
    }
    [Fact]
    public void SessionAndResultSurviveBackupJsonRoundTrip()
    {
        var (session, manifest, result) = Sample();
        var unit = Read<UnitContent>("Units/unit-01.json");
        var evaluation = Check(session, manifest, result).Evaluation!;
        var imported = new ImportedPracticeResult(PracticeResultValidator.ResultId(result), result.RequestId, DateTimeOffset.UtcNow, result, evaluation);
        var records = new List<LearningRecord>
        {
            new() { CourseId = manifest.CourseId, UnitId = unit.UnitId, ExerciseId = result.TaskId, Kind = LearningRecordKind.AiPracticeSession, Id = session.Request.RequestId, Data = JsonSerializer.SerializeToElement(session), UpdatedAt = DateTimeOffset.UtcNow, RecordSchemaVersion = 1 },
            new() { CourseId = manifest.CourseId, UnitId = unit.UnitId, ExerciseId = result.TaskId, Kind = LearningRecordKind.AiAssessmentResult, Id = imported.ResultId, Data = JsonSerializer.SerializeToElement(imported), UpdatedAt = DateTimeOffset.UtcNow, RecordSchemaVersion = 1 }
        };
        var backup = new CourseProgressBackup { Format = "linguaflow-course-progress", BackupSchemaVersion = 1, CourseId = manifest.CourseId, ContentVersion = manifest.ContentVersion, ContentFingerprint = manifest.ContentFingerprint, ExportedAt = DateTimeOffset.UtcNow, Progress = [], Attempts = [], ReviewStates = [], Notes = [], Bookmarks = [], StudySessions = [], AiPracticeResults = records, CourseSettings = [], Drafts = [], VocabularyStates = [], LearningGoals = [] };
        var copy = JsonSerializer.Deserialize<CourseProgressBackup>(JsonSerializer.Serialize(backup))!;
        Assert.Empty(new BackupValidator().Validate(copy, manifest, new Dictionary<string, UnitContent> { [unit.UnitId] = unit }));
        Assert.Equal(2, copy.AiPracticeResults.Count);
    }
    [Fact]
    public void UnitCompletionUsesPassingAttemptWithoutDiscardingLaterWeakAttempt()
    {
        var (session, manifest, result) = Sample();
        var rule = new ThresholdRule { UnitId = result.UnitId, Skill = result.Skill, Text = "Speaking pass at 80%", MinimumPercent = 80 };
        var passing = Check(session, manifest, result, rule).Evaluation!;
        var weakResult = result with { CriterionAssessments = result.CriterionAssessments.Select(c => c with { AwardedPoints = 0 }).ToList() };
        var weak = Check(session, manifest, weakResult, rule).Evaluation!;
        var history = new List<ImportedPracticeResult> { new("result-a", result.RequestId, DateTimeOffset.UtcNow.AddDays(-1), result, passing), new("result-b", result.RequestId, DateTimeOffset.UtcNow, weakResult, weak) };
        var assessment = new UnitCompletionService().Evaluate(result.UnitId, [], [], history);
        Assert.Equal(SkillStatus.Passed, assessment.Skills.Single(s => s.Skill == "speaking").Status);
        Assert.Equal(2, history.Count);
    }
}
