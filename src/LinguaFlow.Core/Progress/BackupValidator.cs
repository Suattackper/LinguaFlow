using System.Text.Json;
using System.Text.RegularExpressions;
using LinguaFlow.Core.Courses;
using LinguaFlow.Core.Vocabulary;
using LinguaFlow.Core.Practice;

namespace LinguaFlow.Core.Progress;

public sealed class BackupValidator
{
    private static readonly Regex IdPattern = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    public IReadOnlyList<string> Validate(CourseProgressBackup? backup, CourseManifest manifest, IReadOnlyDictionary<string, UnitContent> units)
    {
        var errors = new List<string>();
        if (backup is null) return ["Backup JSON is empty or invalid."];
        if (backup.Format != "linguaflow-course-progress") errors.Add("Unsupported backup format.");
        if (backup.BackupSchemaVersion != 1) errors.Add("Unsupported backup schema version.");
        if (backup.CourseId != manifest.CourseId) errors.Add("Backup belongs to a different course.");
        if (backup.ContentVersion != manifest.ContentVersion) errors.Add("Content version differs. No migration is defined.");
        if (backup.ContentFingerprint != manifest.ContentFingerprint) errors.Add("Content fingerprint differs. No migration is defined.");
        if (backup.ExportedAt == default) errors.Add("Export date is missing.");
        var arrays = new (List<LearningRecord>? Records, LearningRecordKind[] Allowed)[]
        {
            (backup.Progress, [LearningRecordKind.CourseProgress, LearningRecordKind.UnitProgress, LearningRecordKind.SectionProgress]),
            (backup.Attempts, [LearningRecordKind.ExerciseAttempt]),
            (backup.ReviewStates, [LearningRecordKind.ReviewSchedule]),
            (backup.Notes, [LearningRecordKind.Note]),
            (backup.Bookmarks, [LearningRecordKind.Bookmark]),
            (backup.StudySessions, [LearningRecordKind.StudySession]),
            (backup.AiPracticeResults, [LearningRecordKind.AiPracticeSession, LearningRecordKind.AiAssessmentResult]),
            (backup.CourseSettings, [LearningRecordKind.CourseSetting]),
            (backup.Drafts, [LearningRecordKind.WritingDraft]),
            (backup.VocabularyStates, [LearningRecordKind.VocabularyState]),
            (backup.LearningGoals, [LearningRecordKind.LearningGoal])
            ,(backup.Mistakes, [LearningRecordKind.Mistake])
        };
        var all = new List<LearningRecord>();
        foreach (var (records, allowed) in arrays)
        {
            if (records is null) { errors.Add("A required record array is missing."); continue; }
            foreach (var record in records)
            {
                if (record is null) { errors.Add("Null record is not allowed."); continue; }
                all.Add(record);
                if (!allowed.Contains(record.Kind)) errors.Add($"Record {record.Id} is in the wrong backup group.");
                if (record.CourseId != backup.CourseId) errors.Add($"Record {record.Id} has a different course ID.");
                if (!ValidId(record.Id)) errors.Add($"Invalid record ID: {record.Id}.");
                if (record.RecordSchemaVersion != 1) errors.Add($"Unsupported record schema: {record.Id}.");
                if (record.UpdatedAt == default) errors.Add($"Missing update time: {record.Id}.");
                if (record.Data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) errors.Add($"Missing data for {record.Id}.");
                if (record.Kind is LearningRecordKind.UnitProgress or LearningRecordKind.SectionProgress or LearningRecordKind.Note or LearningRecordKind.Bookmark or LearningRecordKind.WritingDraft or LearningRecordKind.ExerciseAttempt or LearningRecordKind.VocabularyState or LearningRecordKind.ReviewSchedule or LearningRecordKind.Mistake && record.UnitId is null) errors.Add($"Missing unit ID: {record.Id}.");
                if (record.Kind is LearningRecordKind.SectionProgress or LearningRecordKind.Note or LearningRecordKind.Bookmark && record.SectionId is null) errors.Add($"Missing section ID: {record.Id}.");
                if (record.Kind is LearningRecordKind.WritingDraft or LearningRecordKind.ExerciseAttempt or LearningRecordKind.Mistake && record.ExerciseId is null) errors.Add($"Missing exercise ID: {record.Id}.");
                if (record.Kind is LearningRecordKind.VocabularyState && record.VocabularyId is null) errors.Add($"Missing vocabulary ID: {record.Id}.");
                if (record.UnitId is { } unitId && !units.ContainsKey(unitId)) errors.Add($"Unknown unit ID: {unitId}.");
                if (record.SectionId is { } sectionId && (record.UnitId is null || !units.TryGetValue(record.UnitId, out var unit) || !unit.Sections.Any(s => s.SectionId == sectionId))) errors.Add($"Unknown section ID: {sectionId}.");
                if (record.ExerciseId is { } exerciseId && (record.UnitId is null || !units.TryGetValue(record.UnitId, out var exerciseUnit) || !exerciseUnit.Sections.SelectMany(s => s.Blocks).Any(b => b switch { QuestionBlock q => q.ExerciseId == exerciseId, ExerciseGroupBlock g => g.ExerciseId == exerciseId, WritingBlock w => w.ExerciseId == exerciseId, SpeakingBlock s => s.ExerciseId == exerciseId, ListeningBlock l => l.ExerciseId == exerciseId, _ => false }))) errors.Add($"Unknown exercise ID: {exerciseId}.");
                if (record.VocabularyId is { } vocabularyId && (record.UnitId is null || !units.TryGetValue(record.UnitId, out var vocabularyUnit) || !vocabularyUnit.Sections.SelectMany(s => s.Blocks).OfType<VocabularyBlock>().SelectMany(b => b.Entries ?? []).Any(v => v.VocabularyId == vocabularyId))) errors.Add($"Unknown vocabulary ID: {vocabularyId}.");
                if (record.Kind == LearningRecordKind.VocabularyState)
                {
                    try
                    {
                        var state = record.Data.Deserialize<VocabularyReviewState>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
                        if (state is null || state.CourseId != record.CourseId || state.UnitId != record.UnitId || state.VocabularyId != record.VocabularyId || state.ReviewStage < 0 || state.ReviewCount < 0 || state.SuccessCount < 0 || state.FailureCount < 0 || state.ReviewCount != state.SuccessCount + state.FailureCount || state.History is null || state.History.Count > state.ReviewCount) errors.Add($"Invalid vocabulary review state: {record.Id}.");
                    }
                    catch (JsonException) { errors.Add($"Invalid vocabulary review state: {record.Id}."); }
                }
                if (record.Kind == LearningRecordKind.AiPracticeSession)
                {
                    try
                    {
                        var session = record.Data.Deserialize<PracticeSession>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
                        if (session is null || session.Request.CourseId != record.CourseId || session.Request.UnitId != record.UnitId || session.Request.TaskId != record.ExerciseId || session.Request.RequestId != record.Id || session.Request.CourseContentVersion != manifest.ContentVersion || session.Request.CourseContentFingerprint != manifest.ContentFingerprint || session.PackageMarkdown.Length > PracticePackageBuilder.MaximumBytes) errors.Add($"Invalid AI practice session: {record.Id}.");
                    }
                    catch (Exception ex) when (ex is JsonException or NullReferenceException) { errors.Add($"Invalid AI practice session: {record.Id}."); }
                }
                if (record.Kind == LearningRecordKind.AiAssessmentResult)
                {
                    try
                    {
                        var result = record.Data.Deserialize<ImportedPracticeResult>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
                        if (result is null || result.ResultId != record.Id || result.Result.CourseId != record.CourseId || result.Result.UnitId != record.UnitId || result.Result.TaskId != record.ExerciseId || result.RequestId != result.Result.RequestId || result.ImportSchemaVersion != 1 || result.Result.Format != "linguaflow-practice-result" || result.Result.SchemaVersion != 1 || result.Result.CourseContentVersion != manifest.ContentVersion) errors.Add($"Invalid AI assessment result: {record.Id}.");
                    }
                    catch (Exception ex) when (ex is JsonException or NullReferenceException) { errors.Add($"Invalid AI assessment result: {record.Id}."); }
                }
            }
        }
        foreach (var duplicate in all.GroupBy(x => x.StorageKey).Where(x => x.Count() > 1)) errors.Add($"Duplicate record key: {duplicate.Key}.");
        var sessionIds = all.Where(x => x.Kind == LearningRecordKind.AiPracticeSession).Select(x => x.Id).ToHashSet();
        foreach (var record in all.Where(x => x.Kind == LearningRecordKind.AiAssessmentResult))
        {
            try { var result = record.Data.Deserialize<ImportedPracticeResult>(new JsonSerializerOptions(JsonSerializerDefaults.Web)); if (result is not null && !sessionIds.Contains(result.RequestId)) errors.Add($"AI result {record.Id} has no matching practice session."); }
            catch (JsonException) { }
        }
        return errors;
    }

    public static bool ValidId(string? id) => id is { Length: > 0 and <= 120 } && IdPattern.IsMatch(id);
}
