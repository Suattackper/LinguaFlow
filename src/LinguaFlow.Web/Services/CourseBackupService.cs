using System.Text.Json;
using LinguaFlow.Core.Courses;
using LinguaFlow.Core.Progress;

namespace LinguaFlow.Web.Services;

public sealed record BackupPreview(CourseProgressBackup Backup, int IncomingCount, int ExistingCount, IReadOnlyList<string> Errors);

public sealed class CourseBackupService(LearningRepository repository, UnitContentService units, BackupValidator validator)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public async Task<(string Filename, string Json, int Count)> ExportAsync(CourseManifest manifest)
    {
        var records = await repository.GetCourseAsync(manifest.CourseId);
        var now = DateTimeOffset.UtcNow;
        var backup = CreateBackup(manifest, records, now);
        var json = JsonSerializer.Serialize(backup, JsonOptions);
        var filename = $"linguaflow-{manifest.CourseId}-progress-{now:yyyy-MM-dd}.json";
        await repository.DownloadAsync(filename, json);
        await repository.SetLastBackupAsync(manifest.CourseId, now);
        return (filename, json, records.Count);
    }
    public static CourseProgressBackup CreateBackup(CourseManifest manifest, IReadOnlyList<LearningRecord> records, DateTimeOffset exportedAt)
    {
        List<LearningRecord> Select(params LearningRecordKind[] kinds) => records.Where(x => x.CourseId == manifest.CourseId && kinds.Contains(x.Kind)).OrderBy(x => x.StorageKey, StringComparer.Ordinal).ToList();
        return new CourseProgressBackup
        {
            Format = "linguaflow-course-progress", BackupSchemaVersion = 1,
            CourseId = manifest.CourseId, ContentVersion = manifest.ContentVersion,
            ContentFingerprint = manifest.ContentFingerprint, ExportedAt = exportedAt,
            Progress = Select(LearningRecordKind.CourseProgress, LearningRecordKind.UnitProgress, LearningRecordKind.SectionProgress),
            Attempts = Select(LearningRecordKind.ExerciseAttempt), ReviewStates = Select(LearningRecordKind.ReviewSchedule),
            Notes = Select(LearningRecordKind.Note), Bookmarks = Select(LearningRecordKind.Bookmark),
            StudySessions = Select(LearningRecordKind.StudySession),
            AiPracticeResults = Select(LearningRecordKind.AiPracticeSession, LearningRecordKind.AiAssessmentResult),
            CourseSettings = Select(LearningRecordKind.CourseSetting), Drafts = Select(LearningRecordKind.WritingDraft),
            VocabularyStates = Select(LearningRecordKind.VocabularyState), LearningGoals = Select(LearningRecordKind.LearningGoal), Mistakes = Select(LearningRecordKind.Mistake)
        };
    }
    public async Task<BackupPreview> PreviewAsync(string json, CourseCatalogEntry entry, CourseManifest manifest, CancellationToken cancellationToken = default)
    {
        if (json.Length > 5_000_000) return new(null!, 0, 0, ["Backup exceeds the 5 MB limit."]);
        CourseProgressBackup? backup;
        try { backup = JsonSerializer.Deserialize<CourseProgressBackup>(json, JsonOptions); }
        catch (JsonException) { return new(null!, 0, 0, ["Backup JSON is invalid."]); }
        if (backup is null) return new(null!, 0, 0, ["Backup is empty."]);
        var errors = new List<string>();
        if (backup.CourseId != manifest.CourseId) errors.Add("Backup belongs to a different course.");
        if (backup.ContentVersion != manifest.ContentVersion || backup.ContentFingerprint != manifest.ContentFingerprint) errors.Add("Course content version or fingerprint differs; no migration is defined.");
        if (errors.Count > 0) return new(backup, 0, 0, errors);
        var unitMap = new Dictionary<string, UnitContent>();
        foreach (var reference in manifest.Units)
        {
            cancellationToken.ThrowIfCancellationRequested();
            unitMap[reference.UnitId] = (await units.GetUnitAsync(entry, manifest, reference.UnitId, cancellationToken)).Content;
        }
        errors.AddRange(validator.Validate(backup, manifest, unitMap));
        if (errors.Count > 0) return new(backup, 0, 0, errors);
        var existing = await repository.GetCourseAsync(manifest.CourseId);
        return new(backup, backup.AllRecords().Count(), existing.Count, []);
    }
    public async Task<int> ImportReplaceAsync(BackupPreview preview, CourseCatalogEntry entry, CourseManifest manifest, CancellationToken cancellationToken = default)
    {
        if (preview.Errors.Count > 0) throw new InvalidOperationException("Invalid backup cannot be imported.");
        // Revalidate after preview, before opening a write transaction.
        var unitMap = new Dictionary<string, UnitContent>();
        foreach (var reference in manifest.Units) unitMap[reference.UnitId] = (await units.GetUnitAsync(entry, manifest, reference.UnitId, cancellationToken)).Content;
        var errors = validator.Validate(preview.Backup, manifest, unitMap);
        if (errors.Count > 0) throw new InvalidOperationException(string.Join(" ", errors));
        var existing = await repository.GetCourseAsync(manifest.CourseId);
        var incoming = preview.Backup.AllRecords().ToList();
        await repository.ReplaceCourseAsync(manifest.CourseId, incoming, existing, DateTimeOffset.UtcNow);
        return incoming.Count;
    }
    public async Task<int> RestoreSnapshotAsync(string courseId)
    {
        var snapshot = await repository.GetSnapshotAsync(courseId) ?? throw new InvalidOperationException("No recovery snapshot is available.");
        var current = await repository.GetCourseAsync(courseId);
        await repository.ReplaceCourseAsync(courseId, snapshot.Records, current, DateTimeOffset.UtcNow);
        return snapshot.Records.Count;
    }
}
