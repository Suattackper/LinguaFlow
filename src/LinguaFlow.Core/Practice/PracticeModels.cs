namespace LinguaFlow.Core.Practice;

public sealed record PracticeCriterion(string CriterionId, string Title, int MaxPoints, string EvidenceRequirement);

public sealed record PracticeRequest
{
    public string Format { get; init; } = "linguaflow-practice-request";
    public int SchemaVersion { get; init; } = 1;
    public required string RequestId { get; init; }
    public required string CourseId { get; init; }
    public required string CourseContentVersion { get; init; }
    public required string CourseContentFingerprint { get; init; }
    public required string UnitId { get; init; }
    public required string TaskId { get; init; }
    public required string Skill { get; init; }
    public required string PracticeType { get; init; }
    public required string RubricId { get; init; }
    public int RubricVersion { get; init; } = 1;
    public required DateTimeOffset GeneratedAt { get; init; }
    public required string LearnerDisplayLanguage { get; init; }
    public required string TargetLanguage { get; init; }
    public required Dictionary<string, string> LearnerPreferences { get; init; }
    public required string Difficulty { get; init; }
    public required string TimeGuidance { get; init; }
    public required string CourseTitle { get; init; }
    public required string UnitTitle { get; init; }
    public required string LearningLevel { get; init; }
    public required List<string> LearningObjectives { get; init; }
    public required string PracticeInstructions { get; init; }
    public required List<string> RelevantVocabulary { get; init; }
    public required List<string> RelevantGrammar { get; init; }
    public required string SourceTaskInformation { get; init; }
    public required string OriginalRubric { get; init; }
    public required string CourseTeacherInstructions { get; init; }
    public required List<PracticeCriterion> AssessmentCriteria { get; init; }
    public required List<string> RequiredEvidence { get; init; }
    public int ExpectedResultSchemaVersion { get; init; } = 1;
}

public enum PracticeSessionStatus { Created, Exported, AwaitingResult, ResultImported, Cancelled }
public sealed record PracticeSession
{
    public required PracticeRequest Request { get; init; }
    public required string PackageMarkdown { get; init; }
    public required PracticeSessionStatus Status { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

public sealed record PracticeResult
{
    public required string Format { get; init; }
    public required int SchemaVersion { get; init; }
    public required string RequestId { get; init; }
    public required string CourseId { get; init; }
    public required string CourseContentVersion { get; init; }
    public required string UnitId { get; init; }
    public required string TaskId { get; init; }
    public required string Skill { get; init; }
    public required string RubricId { get; init; }
    public required int RubricVersion { get; init; }
    public required DateTimeOffset CompletedAt { get; init; }
    public required string Evaluator { get; init; }
    public required PracticeEvidence Evidence { get; init; }
    public required List<PracticeCriterionAssessment> CriterionAssessments { get; init; }
    public required List<string> Strengths { get; init; }
    public required List<PracticeMistake> Mistakes { get; init; }
    public required List<string> VocabularySuggestions { get; init; }
    public required List<string> ImprovementSuggestions { get; init; }
    public required string Summary { get; init; }
}
public sealed record PracticeEvidence(string Modality, string Description);
public sealed record PracticeCriterionAssessment(string CriterionId, int? AwardedPoints, int MaxPoints, string EvidenceLevel, string EvidenceSummary);
public sealed record PracticeMistake(string Original, string Correction, string Explanation);
