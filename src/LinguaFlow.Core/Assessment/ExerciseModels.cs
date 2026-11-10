namespace LinguaFlow.Core.Assessment;

public enum ExerciseKind { SingleChoice, MultipleSelect, TrueFalse, FillBlank, MultipleBlanks, Matching, SentenceOrdering, WordOrdering, Reading, ShortAnswer, ErrorCorrection, Writing, Speaking, Listening }
public enum ExerciseMode { Practice, Test, Review }
public enum GradingStatus { Correct, Incorrect, Partial, NeedsReview, Submitted, InsufficientEvidence }
public enum SkillStatus { NotStarted, InProgress, Submitted, NeedsReview, Passed, Completed, InsufficientEvidence, ManuallyReviewed, ExternallyAssessed }

public sealed record ExerciseDefinition
{
    public required string CourseId { get; init; }
    public required string UnitId { get; init; }
    public required string ExerciseId { get; init; }
    public required ExerciseKind Kind { get; init; }
    public required string Prompt { get; init; }
    public required string Skill { get; init; }
    public IReadOnlyList<string> Options { get; init; } = [];
    public IReadOnlyList<string> ValidAnswers { get; init; } = [];
    public string? Explanation { get; init; }
    public bool AutomaticGrading { get; init; }
    public bool AllowPartialCredit { get; init; }
    public bool IgnorePunctuation { get; init; }
    public bool CaseInsensitive { get; init; } = true;
    public decimal MaximumScore { get; init; } = 1;
}

public sealed record ExerciseResponse
{
    public required string ExerciseId { get; init; }
    public required IReadOnlyList<string> Answers { get; init; }
    public bool AudioEvidenceProvided { get; init; }
}

public sealed record ExerciseFeedback
{
    public required GradingStatus Status { get; init; }
    public required decimal EarnedScore { get; init; }
    public required decimal MaximumScore { get; init; }
    public required string Message { get; init; }
    public string? CorrectAnswer { get; init; }
    public string? Explanation { get; init; }
}

public sealed record ExerciseAttempt
{
    public required string AttemptId { get; init; }
    public required string CourseId { get; init; }
    public required string UnitId { get; init; }
    public required string ExerciseId { get; init; }
    public required DateTimeOffset SubmittedAt { get; init; }
    public required ExerciseMode Mode { get; init; }
    public required IReadOnlyList<string> Answers { get; init; }
    public required ExerciseFeedback Feedback { get; init; }
}

public sealed record MistakeEntry
{
    public required string CourseId { get; init; }
    public required string UnitId { get; init; }
    public required string ExerciseId { get; init; }
    public required string LearnerAnswer { get; init; }
    public required string CorrectAnswer { get; init; }
    public required string Explanation { get; init; }
    public required string Skill { get; init; }
    public required DateTimeOffset AttemptDate { get; init; }
    public bool Reviewed { get; init; }
}

public sealed record SkillAssessment(string Skill, SkillStatus Status, decimal Earned, decimal Maximum, string Requirement);
public sealed record UnitAssessment(string UnitId, SkillStatus Status, IReadOnlyList<SkillAssessment> Skills, IReadOnlyList<string> RemainingRequirements);
