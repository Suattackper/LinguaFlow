using System.Text.Json;
using LinguaFlow.Core.Analytics;
using LinguaFlow.Core.Assessment;
using LinguaFlow.Core.Courses;
using LinguaFlow.Core.Progress;
using LinguaFlow.Core.Vocabulary;
using Xunit;

namespace LinguaFlow.Core.Tests;

public sealed class LearningAnalyticsTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Now;
    private static CourseManifest Manifest() => CourseFormatTests.Manifest() with { Units = [.. CourseFormatTests.Manifest().Units, new UnitReference { UnitId = "unit-02", Title = "Unit two", Order = 2, Path = "Units/unit-02.json" }] };
    private static LearningRecord Record(LearningRecordKind kind, string id, object data, string course = "sample", string? unit = "unit-01") => new() { CourseId = course, Kind = kind, Id = id, UnitId = unit, Data = JsonSerializer.SerializeToElement(data), UpdatedAt = Now, RecordSchemaVersion = 1 };
    private static CourseAnalytics Calculate(IReadOnlyList<LearningRecord>? rows = null, IReadOnlyList<VocabularyReviewState>? words = null, IReadOnlyList<ExerciseAttempt>? attempts = null, IReadOnlyList<StudyActivity>? activities = null, DateTimeOffset? backup = null) => LearningAnalyticsCalculator.Calculate(Manifest(), rows ?? [], words ?? [], attempts ?? [], [], activities ?? [], new LearningGoals("sample"), Now, backup);
    [Fact]
    public void EmptyDashboardHasNoFabricatedProgressOrActivity()
    {
        var snapshot = Calculate();
        Assert.Equal(0, snapshot.CompletedUnits);
        Assert.Null(snapshot.LastStudyAt);
        Assert.Null(snapshot.ExerciseAccuracy);
        Assert.Equal(0, snapshot.CurrentStreak);
        Assert.Empty(snapshot.Achievements);
        Assert.Equal("unit-01", snapshot.CurrentUnitId);
        Assert.Single(snapshot.Recommendations);
    }
    [Fact]
    public void CompletedUnitAndResumePositionAreCalculatedFromSavedRecords()
    {
        var complete = new UnitAssessment("unit-01", SkillStatus.Completed, [], []);
        var rows = new[] { Record(LearningRecordKind.UnitProgress, "unit-01", complete), Record(LearningRecordKind.CourseProgress, "position", "section-02", unit: "unit-02") };
        var snapshot = Calculate(rows);
        Assert.Equal(1, snapshot.CompletedUnits);
        Assert.Equal(50, snapshot.CompletionPercent);
        Assert.Equal("unit-02", snapshot.CurrentUnitId);
        Assert.Contains(snapshot.Achievements, a => a.Id == "first-unit");
    }
    [Fact]
    public void DueVocabularyAccuracyAndCourseIsolationUseRealAttempts()
    {
        var word = new VocabularyReviewState { CourseId = "sample", UnitId = "unit-01", VocabularyId = "word-01", Status = VocabularyLearningStatus.Known, ReviewCount = 1, SuccessCount = 1, NextDueAt = Now.AddHours(-1) };
        var foreign = word with { CourseId = "other", VocabularyId = "word-02" };
        var correct = Attempt("sample", "unit-01-c1-q1", GradingStatus.Correct);
        var wrong = Attempt("sample", "unit-01-e1-q1", GradingStatus.Incorrect);
        var snapshot = Calculate(words: [word, foreign], attempts: [correct, wrong, Attempt("other", "unit-01-c1-q2", GradingStatus.Correct)]);
        Assert.Equal(1, snapshot.DueVocabulary);
        Assert.Equal(1, snapshot.KnownVocabulary);
        Assert.Equal(50, snapshot.ExerciseAccuracy);
        Assert.Equal(100, snapshot.GrammarAccuracy);
        Assert.Equal(0, snapshot.ReadingAccuracy);
        Assert.Contains(snapshot.Recommendations, r => r.Id == "review" && r.Href.Contains("sample"));
    }
    [Fact]
    public void StreakGoalsAndEstimatedMinutesOnlyUseExplicitSessions()
    {
        var activities = new[] { new StudyActivity("sample", "unit-01", StudyActivityKind.LessonOpened, Now.AddDays(-1)), new StudyActivity("sample", "unit-01", StudyActivityKind.SessionCompleted, Now, 15), new StudyActivity("other", "unit-01", StudyActivityKind.SessionCompleted, Now, 90) };
        var snapshot = Calculate(activities: activities, backup: Now.AddDays(-20));
        Assert.Equal(2, snapshot.CurrentStreak);
        Assert.Equal(15, snapshot.TodayEstimatedMinutes);
        Assert.Equal(20, snapshot.DaysSinceBackup);
        Assert.Equal(Now.ToLocalTime().DayOfWeek == DayOfWeek.Monday ? 1 : 2, snapshot.WeeklyStudyDays);
    }
    [Fact]
    public void SevenRealReviewEventsUnlockReviewMilestone()
    {
        var activities = Enumerable.Range(0, 7).Select(i => new StudyActivity("sample", "unit-01", StudyActivityKind.VocabularyReviewed, Now.AddMinutes(-i))).ToList();
        Assert.Contains(Calculate(activities: activities).Achievements, a => a.Id == "seven-reviews");
        Assert.DoesNotContain(Calculate(activities: activities.Take(6).ToList()).Achievements, a => a.Id == "seven-reviews");
    }
    [Fact]
    public void RestoredRecordsProduceTheSameVisualization()
    {
        var rows = new[] { Record(LearningRecordKind.UnitProgress, "unit-01", new UnitAssessment("unit-01", SkillStatus.Completed, [], [])), Record(LearningRecordKind.CourseProgress, "position", "section-01", unit: "unit-02") };
        var restored = JsonSerializer.Deserialize<LearningRecord[]>(JsonSerializer.Serialize(rows))!;
        var before = Calculate(rows); var after = Calculate(restored);
        Assert.Equal(before.CompletedUnits, after.CompletedUnits);
        Assert.Equal(before.CompletionPercent, after.CompletionPercent);
        Assert.Equal(before.CurrentUnitId, after.CurrentUnitId);
    }
    [Fact]
    public void DailyPlanRespectsConfiguredTimeAndUsesOnlyWorkingRoutes()
    {
        var due = new VocabularyReviewState { CourseId = "sample", UnitId = "unit-01", VocabularyId = "word-01", NextDueAt = Now.AddMinutes(-1) };
        var snapshot = LearningAnalyticsCalculator.Calculate(Manifest(), [], [due], [], [], [], new LearningGoals("sample", DailyMinutes: 15, SessionMinutes: 30), Now, null);
        Assert.True(snapshot.Recommendations.Sum(r => r.SuggestedMinutes) <= 15);
        Assert.All(snapshot.Recommendations, r => Assert.StartsWith("courses/sample/", r.Href));
        Assert.Equal("review", snapshot.Recommendations[0].Id);
    }
    private static ExerciseAttempt Attempt(string course, string id, GradingStatus status) => new() { AttemptId = Guid.NewGuid().ToString("N"), CourseId = course, UnitId = "unit-01", ExerciseId = id, SubmittedAt = Now, Mode = ExerciseMode.Practice, Answers = [], Feedback = new ExerciseFeedback { Status = status, EarnedScore = status == GradingStatus.Correct ? 1 : 0, MaximumScore = 1, Message = "" } };
}
