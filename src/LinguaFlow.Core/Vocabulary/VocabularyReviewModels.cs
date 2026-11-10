namespace LinguaFlow.Core.Vocabulary;

public enum VocabularyLearningStatus { New, Learning, Known, Difficult }

public sealed record VocabularyReviewEvent(DateTimeOffset ReviewedAt, bool Remembered, int StageAfter, DateTimeOffset NextDueAt);

public sealed record VocabularyReviewState
{
    public required string CourseId { get; init; }
    public required string VocabularyId { get; init; }
    public required string UnitId { get; init; }
    public VocabularyLearningStatus Status { get; init; } = VocabularyLearningStatus.New;
    public int ReviewStage { get; init; }
    public DateTimeOffset? LastReviewedAt { get; init; }
    public DateTimeOffset? NextDueAt { get; init; }
    public int ReviewCount { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public bool Favorite { get; init; }
    public bool InReviewQueue { get; init; }
    public string Note { get; init; } = "";
    public IReadOnlyList<VocabularyReviewEvent> History { get; init; } = [];
}

public sealed record ReviewDashboardCounts(int DueToday, int Overdue, int NewWords, int DifficultWords, int ReviewedRecently);
public sealed record VocabularySessionSummary(string CourseId, DateTimeOffset StartedAt, DateTimeOffset CompletedAt, int Reviewed, int Remembered, int Forgotten);
