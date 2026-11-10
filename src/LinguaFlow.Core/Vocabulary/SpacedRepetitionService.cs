namespace LinguaFlow.Core.Vocabulary;

public sealed class SpacedRepetitionService
{
    public static readonly int[] DefaultIntervals = [1, 3, 7, 14];

    public VocabularyReviewState Review(VocabularyReviewState state, bool remembered, DateTimeOffset reviewedAt, IReadOnlyList<int>? unitIntervals = null)
    {
        var intervals = NormalizeIntervals(unitIntervals);
        var stage = remembered ? Math.Min(state.ReviewStage + 1, intervals.Count) : Math.Max(0, state.ReviewStage - 1);
        var dueAt = remembered ? reviewedAt.ToUniversalTime().AddDays(intervals[Math.Max(0, stage - 1)]) : reviewedAt.ToUniversalTime();
        var status = remembered ? stage >= intervals.Count ? VocabularyLearningStatus.Known : VocabularyLearningStatus.Learning : VocabularyLearningStatus.Difficult;
        var history = state.History.Append(new VocabularyReviewEvent(reviewedAt.ToUniversalTime(), remembered, stage, dueAt)).ToArray();
        return state with
        {
            ReviewStage = stage, LastReviewedAt = reviewedAt.ToUniversalTime(), NextDueAt = dueAt,
            ReviewCount = state.ReviewCount + 1, SuccessCount = state.SuccessCount + (remembered ? 1 : 0),
            FailureCount = state.FailureCount + (remembered ? 0 : 1), Status = status, InReviewQueue = false, History = history
        };
    }

    public static bool IsDue(VocabularyReviewState state, DateTimeOffset now) => state.NextDueAt is { } due && due <= now.ToUniversalTime();
    public static bool IsOverdue(VocabularyReviewState state, DateTimeOffset now, TimeZoneInfo zone) => state.NextDueAt is { } due && TimeZoneInfo.ConvertTime(due, zone).Date < TimeZoneInfo.ConvertTime(now, zone).Date;
    public static bool IsDueToday(VocabularyReviewState state, DateTimeOffset now, TimeZoneInfo zone) => state.NextDueAt is { } due && TimeZoneInfo.ConvertTime(due, zone).Date == TimeZoneInfo.ConvertTime(now, zone).Date;

    public ReviewDashboardCounts Count(IEnumerable<VocabularyReviewState> states, int totalWords, DateTimeOffset now, TimeZoneInfo zone)
    {
        var list = states.ToList();
        return new ReviewDashboardCounts(list.Count(s => IsDueToday(s, now, zone)), list.Count(s => IsOverdue(s, now, zone)),
            Math.Max(0, totalWords - list.Count(s => s.ReviewCount > 0)), list.Count(s => s.Status == VocabularyLearningStatus.Difficult),
            list.Count(s => s.LastReviewedAt is { } date && date >= now.AddDays(-7)));
    }

    public static IReadOnlyList<int> NormalizeIntervals(IReadOnlyList<int>? unitIntervals)
    {
        if (unitIntervals is null || unitIntervals.Count == 0) return DefaultIntervals;
        if (unitIntervals.Any(x => x <= 0) || unitIntervals.Zip(unitIntervals.Skip(1)).Any(x => x.First >= x.Second)) throw new ArgumentException("Review intervals must be positive and increasing.");
        return unitIntervals;
    }
}
