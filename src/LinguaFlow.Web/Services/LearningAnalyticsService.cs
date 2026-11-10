using System.Text.Json;
using LinguaFlow.Core.Analytics;
using LinguaFlow.Core.Assessment;
using LinguaFlow.Core.Courses;
using LinguaFlow.Core.Practice;
using LinguaFlow.Core.Progress;
using LinguaFlow.Core.Vocabulary;

namespace LinguaFlow.Web.Services;

public sealed class LearningAnalyticsService(LearningRepository repository, VocabularyContentService vocabulary)
{
    public async Task<CourseAnalytics> GetAsync(CourseCatalogEntry entry, CourseManifest manifest, CancellationToken ct = default)
    {
        var records = await repository.GetCourseAsync(manifest.CourseId);
        var words = Read<VocabularyReviewState>(records, LearningRecordKind.VocabularyState).Where(w => w.History is not null && w.ReviewCount >= 0 && w.CourseId == manifest.CourseId).ToList();
        var attempts = Read<ExerciseAttempt>(records, LearningRecordKind.ExerciseAttempt).Where(a => a.Feedback is not null && a.CourseId == manifest.CourseId).ToList();
        var results = Read<ImportedPracticeResult>(records, LearningRecordKind.AiAssessmentResult).Where(r => r.Result is not null && r.Evaluation is not null && r.Result.CourseId == manifest.CourseId).ToList();
        var activities = Read<StudyActivity>(records, LearningRecordKind.StudySession).Where(a => a.CourseId == manifest.CourseId && a.At != default).ToList();
        var goals = Read<LearningGoals>(records, LearningRecordKind.LearningGoal).FirstOrDefault(g => g.CourseId == manifest.CourseId) ?? new LearningGoals(manifest.CourseId);
        var backup = await repository.GetLastBackupAsync(manifest.CourseId);
        var analytics = LearningAnalyticsCalculator.Calculate(manifest, records, words, attempts, results, activities, goals, DateTimeOffset.UtcNow, backup);
        try { return LearningAnalyticsCalculator.WithVocabularyTotal(analytics, (await vocabulary.GetAsync(entry, manifest, ct)).Entries.Count); }
        catch (Exception ex) when (ex is CourseContentException or HttpRequestException) { return analytics; }
    }
    public async Task<LearningGoals> GetGoalsAsync(string courseId)
    {
        var rows = await repository.LearningGoals.GetByCourseAsync(courseId);
        return Read<LearningGoals>(rows, LearningRecordKind.LearningGoal).FirstOrDefault(g => g.CourseId == courseId) ?? new LearningGoals(courseId);
    }
    public async Task<IReadOnlyList<StudyActivity>> GetActivitiesAsync(string courseId) => Read<StudyActivity>(await repository.StudySessions.GetByCourseAsync(courseId), LearningRecordKind.StudySession).Where(a => a.CourseId == courseId).OrderByDescending(a => a.At).ToList();
    public Task SaveGoalsAsync(LearningGoals goals)
    {
        if (goals.DailyMinutes is < 5 or > 240 || goals.WeeklyDays is < 1 or > 7 || goals.SessionMinutes is < 5 or > 120 || goals.DailyVocabularyReviews is < 0 or > 200 || goals.PreferredTime is not ("any" or "morning" or "afternoon" or "evening") || (goals.TargetCourseId is not null && goals.TargetCourseId != goals.CourseId)) throw new ArgumentException("Invalid learning goals.");
        return repository.LearningGoals.SaveAsync(new LearningRecord { CourseId = goals.CourseId, Kind = LearningRecordKind.LearningGoal, Id = "goals", Data = JsonSerializer.SerializeToElement(goals), UpdatedAt = DateTimeOffset.UtcNow, RecordSchemaVersion = 1 });
    }
    private static List<T> Read<T>(IEnumerable<LearningRecord> records, LearningRecordKind kind) where T : class
    {
        var result = new List<T>();
        foreach (var record in records.Where(r => r.Kind == kind))
        {
            try { if (record.Data.Deserialize<T>() is { } value) result.Add(value); }
            catch (JsonException) { }
        }
        return result;
    }
}

public sealed class StudyActivityService(LearningRepository repository)
{
    public Task RecordAsync(string courseId, string unitId, StudyActivityKind kind, int? estimatedMinutes = null, string? sectionId = null)
    {
        if (!BackupValidator.ValidId(courseId) || !BackupValidator.ValidId(unitId) || (sectionId is not null && !BackupValidator.ValidId(sectionId)) || estimatedMinutes is < 0 or > 120) throw new ArgumentException("Invalid study activity.");
        var now = DateTimeOffset.UtcNow;
        var eventData = new StudyActivity(courseId, unitId, kind, now, estimatedMinutes, sectionId);
        return repository.StudySessions.SaveAsync(new LearningRecord { CourseId = courseId, UnitId = unitId, SectionId = sectionId, Kind = LearningRecordKind.StudySession, Id = "activity-" + Guid.NewGuid().ToString("N"), Data = JsonSerializer.SerializeToElement(eventData), UpdatedAt = now, RecordSchemaVersion = 1 });
    }
    public static LearningRecord Record(string courseId, string unitId, StudyActivityKind kind, DateTimeOffset at) => new()
    {
        CourseId = courseId, UnitId = unitId, Kind = LearningRecordKind.StudySession, Id = "activity-" + Guid.NewGuid().ToString("N"),
        Data = JsonSerializer.SerializeToElement(new StudyActivity(courseId, unitId, kind, at)), UpdatedAt = at, RecordSchemaVersion = 1
    };
}
