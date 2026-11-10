using System.Text.Json;
using LinguaFlow.Core.Assessment;
using LinguaFlow.Core.Courses;
using LinguaFlow.Core.Practice;
using LinguaFlow.Core.Progress;
using LinguaFlow.Core.Vocabulary;

namespace LinguaFlow.Core.Analytics;

public enum StudyActivityKind { LessonOpened, SectionCompleted, ExerciseSubmitted, VocabularyReviewed, PracticeResultImported, SessionCompleted }
public sealed record StudyActivity(string CourseId, string UnitId, StudyActivityKind Kind, DateTimeOffset At, int? EstimatedMinutes = null, string? SectionId = null);
public sealed record LearningGoals(string CourseId, int DailyMinutes = 20, int WeeklyDays = 3, int SessionMinutes = 20, int DailyVocabularyReviews = 10, string PreferredTime = "any", string? TargetCourseId = null);
public sealed record StudyRecommendation(string Id, string Reason, string Href, int SuggestedMinutes);
public sealed record Achievement(string Id, DateTimeOffset EarnedAt);
public sealed record DailyActivity(DateOnly Day, int Events, int EstimatedMinutes);
public sealed record CourseAnalytics(
    string CourseId, int TotalUnits, int CompletedUnits, string? CurrentUnitId, DateTimeOffset? LastStudyAt,
    string? RecentSkill, int DueVocabulary, int KnownVocabulary, int ReviewedVocabulary, int VocabularyTotal,
    int ExerciseCorrect, int ExerciseGraded, int GrammarCorrect, int GrammarGraded, int ReadingCorrect, int ReadingGraded,
    int MistakesToReview, int PendingPracticeResults, int? DaysSinceBackup, int CurrentStreak, int WeeklyStudyDays,
    int TodayEstimatedMinutes, int TodayVocabularyReviews, IReadOnlyList<DailyActivity> DailyActivity,
    IReadOnlyList<StudyRecommendation> Recommendations, IReadOnlyList<Achievement> Achievements,
    IReadOnlyDictionary<string, SkillStatus> UnitStatuses, IReadOnlyDictionary<string, PracticeVerdict> SkillVerdicts)
{
    public decimal CompletionPercent => TotalUnits == 0 ? 0 : Math.Round((decimal)CompletedUnits / TotalUnits * 100, 1);
    public decimal? ExerciseAccuracy => ExerciseGraded == 0 ? null : Math.Round((decimal)ExerciseCorrect / ExerciseGraded * 100, 1);
    public decimal? GrammarAccuracy => GrammarGraded == 0 ? null : Math.Round((decimal)GrammarCorrect / GrammarGraded * 100, 1);
    public decimal? ReadingAccuracy => ReadingGraded == 0 ? null : Math.Round((decimal)ReadingCorrect / ReadingGraded * 100, 1);
    public decimal VocabularyMasteryPercent => VocabularyTotal == 0 ? 0 : Math.Round((decimal)KnownVocabulary / VocabularyTotal * 100, 1);
}

public static class LearningAnalyticsCalculator
{
    public static CourseAnalytics Calculate(CourseManifest manifest, IReadOnlyList<LearningRecord> records, IReadOnlyList<VocabularyReviewState> vocabulary,
        IReadOnlyList<ExerciseAttempt> attempts, IReadOnlyList<ImportedPracticeResult> practiceResults, IReadOnlyList<StudyActivity> activities,
        LearningGoals goals, DateTimeOffset now, DateTimeOffset? lastBackupAt)
    {
        var scoped = records.Where(r => r.CourseId == manifest.CourseId).ToList();
        var validUnits = manifest.Units.Select(u => u.UnitId).ToHashSet();
        var statuses = scoped.Where(r => r.Kind == LearningRecordKind.UnitProgress && r.UnitId is not null && validUnits.Contains(r.UnitId))
            .Select(r => (r.UnitId!, Status: TryUnitStatus(r), r.UpdatedAt)).Where(x => x.Status is not null)
            .GroupBy(x => x.Item1).ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.UpdatedAt).First().Status!.Value);
        var completed = statuses.Count(x => x.Value == SkillStatus.Completed);
        var position = scoped.Where(r => r.Kind == LearningRecordKind.CourseProgress && r.Id == "position" && r.UnitId is not null && validUnits.Contains(r.UnitId)).OrderByDescending(r => r.UpdatedAt).FirstOrDefault();
        var current = position?.UnitId ?? manifest.Units.OrderBy(u => u.Order).FirstOrDefault(u => !statuses.TryGetValue(u.UnitId, out var s) || s != SkillStatus.Completed)?.UnitId;
        var unitActivity = activities.Where(a => a.CourseId == manifest.CourseId && validUnits.Contains(a.UnitId)).ToList();
        var scopedAttempts = attempts.Where(a => a.CourseId == manifest.CourseId && validUnits.Contains(a.UnitId)).ToList();
        var scopedPractice = practiceResults.Where(r => r.Result.CourseId == manifest.CourseId && validUnits.Contains(r.Result.UnitId)).ToList();
        var lastStudy = unitActivity.Select(a => (DateTimeOffset?)a.At).Concat(scopedAttempts.Select(a => (DateTimeOffset?)a.SubmittedAt)).Concat(scopedPractice.Select(a => (DateTimeOffset?)a.ImportedAt)).Concat(vocabulary.Where(v => v.CourseId == manifest.CourseId).Select(v => v.LastReviewedAt)).Append(position?.UpdatedAt).Max();
        var recentSkill = scopedPractice.OrderByDescending(r => r.ImportedAt).Select(r => (Skill: r.Result.Skill, At: r.ImportedAt))
            .Concat(scopedAttempts.Select(a => (Skill: SkillFromExercise(a.ExerciseId), At: a.SubmittedAt)))
            .OrderByDescending(x => x.At).FirstOrDefault().Skill;
        var graded = scopedAttempts.Where(a => a.Feedback.Status is GradingStatus.Correct or GradingStatus.Incorrect or GradingStatus.Partial).ToList();
        var grammar = graded.Where(a => SkillFromExercise(a.ExerciseId) == "grammar").ToList();
        var reading = graded.Where(a => SkillFromExercise(a.ExerciseId) == "reading").ToList();
        var words = vocabulary.Where(v => v.CourseId == manifest.CourseId && validUnits.Contains(v.UnitId) && v.History is not null && v.ReviewCount >= 0).ToList();
        var due = words.Count(w => w.InReviewQueue || SpacedRepetitionService.IsDue(w, now));
        var pending = scoped.Where(r => r.Kind == LearningRecordKind.AiPracticeSession).Count(r => TrySessionStatus(r) is PracticeSessionStatus.Exported or PracticeSessionStatus.AwaitingResult);
        var mistakes = scoped.Count(r => r.Kind == LearningRecordKind.Mistake && !TryReviewed(r));
        var days = unitActivity.Select(a => a.At).Concat(scopedAttempts.Select(a => a.SubmittedAt)).Concat(scopedPractice.Select(a => a.ImportedAt)).Concat(words.SelectMany(w => w.History.Select(h => h.ReviewedAt))).Select(at => DateOnly.FromDateTime(at.ToLocalTime().DateTime)).Distinct().ToHashSet();
        var today = DateOnly.FromDateTime(now.ToLocalTime().DateTime);
        var streak = 0; var cursor = days.Contains(today) ? today : today.AddDays(-1);
        while (days.Contains(cursor)) { streak++; cursor = cursor.AddDays(-1); }
        var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var weekDays = days.Count(d => d >= weekStart && d <= today);
        var daily = days.OrderBy(d => d).Select(day => new DailyActivity(day,
            Math.Max(unitActivity.Count(a => DateOnly.FromDateTime(a.At.ToLocalTime().DateTime) == day), scopedAttempts.Count(a => DateOnly.FromDateTime(a.SubmittedAt.ToLocalTime().DateTime) == day) + scopedPractice.Count(a => DateOnly.FromDateTime(a.ImportedAt.ToLocalTime().DateTime) == day) + words.Sum(w => w.History.Count(h => DateOnly.FromDateTime(h.ReviewedAt.ToLocalTime().DateTime) == day))),
            unitActivity.Where(a => a.Kind == StudyActivityKind.SessionCompleted && DateOnly.FromDateTime(a.At.ToLocalTime().DateTime) == day).Sum(a => a.EstimatedMinutes ?? 0))).ToList();
        var todayMinutes = daily.FirstOrDefault(d => d.Day == today)?.EstimatedMinutes ?? 0;
        var todayReviews = words.Sum(w => w.History.Count(h => DateOnly.FromDateTime(h.ReviewedAt.ToLocalTime().DateTime) == today));
        var skillVerdicts = scopedPractice.GroupBy(r => r.Result.Skill).ToDictionary(g => g.Key, g => g.Any(r => r.Evaluation.Verdict == PracticeVerdict.Passed) ? PracticeVerdict.Passed : g.OrderByDescending(r => r.ImportedAt).First().Evaluation.Verdict);
        var recommendations = Recommend(manifest.CourseId, current, due, mistakes, pending, statuses, skillVerdicts, goals);
        var achievements = Earned(completed, words, scopedPractice, scoped, unitActivity);
        return new(manifest.CourseId, manifest.Units.Count, completed, current, lastStudy, recentSkill, due,
            words.Count(w => w.Status == VocabularyLearningStatus.Known), words.Count(w => w.ReviewCount > 0), 0,
            graded.Count(a => a.Feedback.Status == GradingStatus.Correct), graded.Count,
            grammar.Count(a => a.Feedback.Status == GradingStatus.Correct), grammar.Count,
            reading.Count(a => a.Feedback.Status == GradingStatus.Correct), reading.Count,
            mistakes, pending, lastBackupAt is null ? null : Math.Max(0, (int)(now - lastBackupAt.Value).TotalDays), streak, weekDays,
            todayMinutes, todayReviews, daily, recommendations, achievements, statuses, skillVerdicts);
    }
    public static CourseAnalytics WithVocabularyTotal(CourseAnalytics analytics, int total) => analytics with { VocabularyTotal = total };
    private static SkillStatus? TryUnitStatus(LearningRecord r)
    {
        try { return r.Data.Deserialize<UnitAssessment>()?.Status; } catch (System.Text.Json.JsonException) { return null; }
    }
    private static PracticeSessionStatus? TrySessionStatus(LearningRecord r)
    {
        try { return r.Data.Deserialize<PracticeSession>()?.Status; } catch (System.Text.Json.JsonException) { return null; }
    }
    private static bool TryReviewed(LearningRecord r)
    {
        try { return r.Data.Deserialize<MistakeEntry>()?.Reviewed ?? false; } catch (System.Text.Json.JsonException) { return false; }
    }
    private static string SkillFromExercise(string id) => id.Contains("-c", StringComparison.Ordinal) ? "grammar" : id.Contains("-e", StringComparison.Ordinal) ? "reading" : id.EndsWith("-writing", StringComparison.Ordinal) ? "writing" : id.EndsWith("-speaking", StringComparison.Ordinal) ? "speaking" : "other";
    private static IReadOnlyList<StudyRecommendation> Recommend(string courseId, string? current, int due, int mistakes, int pending, IReadOnlyDictionary<string, SkillStatus> statuses, IReadOnlyDictionary<string, PracticeVerdict> verdicts, LearningGoals goals)
    {
        var list = new List<StudyRecommendation>();
        if (due > 0) list.Add(new("review", $"{due} vocabulary items are due or queued.", $"courses/{courseId}/flashcards?mode=due", Math.Min(15, Math.Max(5, goals.SessionMinutes / 2))));
        if (current is not null) list.Add(new("continue", "Resume the last opened or next unfinished Unit.", $"courses/{courseId}/units/{current}", goals.SessionMinutes));
        if (mistakes > 0) list.Add(new("mistakes", $"{mistakes} saved mistakes have not been marked reviewed.", $"courses/{courseId}/mistakes", 10));
        if (pending > 0) list.Add(new("practice-result", $"{pending} exported practice sessions are awaiting a result.", $"courses/{courseId}/practice/import", 5));
        if (current is not null && statuses.TryGetValue(current, out var status) && status == SkillStatus.NeedsReview) list.Add(new("checkpoint", "The current Unit still has pending completion requirements.", $"courses/{courseId}/units/{current}", 15));
        if (verdicts.Any(x => x.Value is PracticeVerdict.NeedsImprovement or PracticeVerdict.InsufficientEvidence)) list.Add(new("practice", "A previous practice result needs revision or more evidence.", $"courses/{courseId}/practice", 15));
        var remaining = goals.DailyMinutes;
        var plan = new List<StudyRecommendation>();
        foreach (var item in list)
        {
            if (remaining <= 0) break;
            var minutes = Math.Min(item.SuggestedMinutes, remaining);
            plan.Add(item with { SuggestedMinutes = minutes });
            remaining -= minutes;
        }
        return plan;
    }
    private static IReadOnlyList<Achievement> Earned(int completed, IReadOnlyList<VocabularyReviewState> words, IReadOnlyList<ImportedPracticeResult> practice, IReadOnlyList<LearningRecord> records, IReadOnlyList<StudyActivity> activities)
    {
        var list = new List<Achievement>();
        if (completed > 0) list.Add(new("first-unit", records.Where(r => r.Kind == LearningRecordKind.UnitProgress).Max(r => r.UpdatedAt)));
        var reviews = activities.Where(a => a.Kind == StudyActivityKind.VocabularyReviewed).OrderBy(a => a.At).ToList();
        if (reviews.Count >= 7) list.Add(new("seven-reviews", reviews[6].At));
        var speaking = practice.Where(p => p.Result.Skill == "speaking").OrderBy(p => p.ImportedAt).FirstOrDefault();
        if (speaking is not null) list.Add(new("first-speaking", speaking.ImportedAt));
        return list;
    }
}
