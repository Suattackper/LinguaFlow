using System.Text.Json;
using LinguaFlow.Core.Progress;
using Microsoft.JSInterop;

namespace LinguaFlow.Web.Services;

public sealed class LearningStorageException(string message, Exception inner) : Exception(message, inner);
public sealed record StorageEstimate(ulong Usage, ulong Quota);
public sealed record RecoverySnapshot(string CourseId, List<LearningRecord> Records, DateTimeOffset CreatedAt);

public sealed class LearningRepository(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? _module;
    private readonly SemaphoreSlim _moduleLock = new(1, 1);
    public EntityRepository CourseProgress => For(LearningRecordKind.CourseProgress);
    public EntityRepository UnitProgress => For(LearningRecordKind.UnitProgress);
    public EntityRepository SectionProgress => For(LearningRecordKind.SectionProgress);
    public EntityRepository ExerciseAttempts => For(LearningRecordKind.ExerciseAttempt);
    public EntityRepository WritingDrafts => For(LearningRecordKind.WritingDraft);
    public EntityRepository Bookmarks => For(LearningRecordKind.Bookmark);
    public EntityRepository Notes => For(LearningRecordKind.Note);
    public EntityRepository VocabularyStates => For(LearningRecordKind.VocabularyState);
    public EntityRepository ReviewSchedules => For(LearningRecordKind.ReviewSchedule);
    public EntityRepository StudySessions => For(LearningRecordKind.StudySession);
    public EntityRepository LearningGoals => For(LearningRecordKind.LearningGoal);
    public EntityRepository AiPracticeSessions => For(LearningRecordKind.AiPracticeSession);
    public EntityRepository AiAssessmentResults => For(LearningRecordKind.AiAssessmentResult);
    public EntityRepository UserPreferences => For(LearningRecordKind.UserPreference);
    public EntityRepository CourseSettings => For(LearningRecordKind.CourseSetting);
    public EntityRepository Mistakes => For(LearningRecordKind.Mistake);
    public EntityRepository For(LearningRecordKind kind) => new(this, kind);

    private async Task<IJSObjectReference> ModuleAsync()
    {
        if (_module is not null) return _module;
        await _moduleLock.WaitAsync();
        try { return _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/learning-store.js"); }
        catch (JSException ex) { throw new LearningStorageException("IndexedDB could not be opened. Check browser storage permissions.", ex); }
        finally { _moduleLock.Release(); }
    }

    private static object Stored(LearningRecord record) => new
    {
        key = record.StorageKey, record.CourseId, record.Kind, record.Id, record.UnitId,
        record.SectionId, record.ExerciseId, record.VocabularyId, record.Data,
        record.UpdatedAt, record.RecordSchemaVersion
    };
    private async Task<T> Call<T>(string method, params object?[] args)
    {
        try { return await (await ModuleAsync()).InvokeAsync<T>(method, args); }
        catch (JSException ex) { throw Wrap(ex); }
    }
    private async Task Call(string method, params object?[] args)
    {
        try { await (await ModuleAsync()).InvokeVoidAsync(method, args); }
        catch (JSException ex) { throw Wrap(ex); }
    }
    private static LearningStorageException Wrap(JSException ex) => new(ex.Message.Contains("Quota", StringComparison.OrdinalIgnoreCase) ? "Browser storage quota was exceeded. Export a backup and free space." : $"Browser learning storage failed: {ex.Message.Split('\n')[0]}", ex);
    public Task InitializeAsync() => Call("openStore");
    public Task<List<LearningRecord>> GetCourseAsync(string courseId) => Call<List<LearningRecord>>("getCourse", courseId);
    public Task<List<LearningRecord>> GetAllAsync() => Call<List<LearningRecord>>("getAllRecords");
    public Task UpsertAsync(LearningRecord record) { ValidateRecord(record); return Call("putRecord", Stored(record)); }
    public Task UpsertBatchAsync(IReadOnlyList<LearningRecord> records) { foreach (var record in records) ValidateRecord(record); return Call("putBatch", (object)records.Select(Stored).ToArray()); }
    public Task MutateBatchAsync(IReadOnlyList<LearningRecord> records, IReadOnlyList<string> keysToDelete)
    {
        foreach (var record in records) ValidateRecord(record);
        if (keysToDelete.Any(key => string.IsNullOrWhiteSpace(key))) throw new ArgumentException("Invalid record key.");
        return Call("mutateBatch", records.Select(Stored).ToArray(), keysToDelete);
    }
    public Task DeleteAsync(string courseId, LearningRecordKind kind, string id) => Call("deleteRecord", $"{courseId}|{kind}|{id}");
    public Task ReplaceCourseAsync(string courseId, IReadOnlyList<LearningRecord> incoming, IReadOnlyList<LearningRecord> snapshot, DateTimeOffset importedAt)
    {
        foreach (var record in incoming) { ValidateRecord(record); if (record.CourseId != courseId) throw new ArgumentException("Cross-course record is not allowed."); }
        return Call("replaceCourse", courseId, incoming.Select(Stored).ToArray(), snapshot.Select(Stored).ToArray(), importedAt);
    }
    public Task<RecoverySnapshot?> GetSnapshotAsync(string courseId) => Call<RecoverySnapshot?>("getSnapshot", courseId);
    public Task ResetCourseAsync(string courseId) => Call("resetCourse", courseId);
    public Task ResetAllAsync() => Call("resetAll");
    public Task<DateTimeOffset?> GetLastBackupAsync(string courseId) => Call<DateTimeOffset?>("getBackupDate", courseId);
    public Task SetLastBackupAsync(string courseId, DateTimeOffset when) => Call("setBackupDate", courseId, when);
    public Task<StorageEstimate?> EstimateAsync() => Call<StorageEstimate?>("estimateUsage");
    public Task DownloadAsync(string filename, string content) => Call("downloadJson", filename, content);
    private static void ValidateRecord(LearningRecord record)
    {
        if (!BackupValidator.ValidId(record.CourseId) || !BackupValidator.ValidId(record.Id)) throw new ArgumentException("Invalid course or record ID.");
        if (record.RecordSchemaVersion != 1) throw new ArgumentException("Unsupported record schema version.");
        if (record.Data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) throw new ArgumentException("Record data is required.");
    }
    public async ValueTask DisposeAsync() { if (_module is not null) await _module.DisposeAsync(); _moduleLock.Dispose(); }

    public sealed class EntityRepository(LearningRepository parent, LearningRecordKind kind)
    {
        public async Task<IReadOnlyList<LearningRecord>> GetByCourseAsync(string courseId) => (await parent.GetCourseAsync(courseId)).Where(x => x.Kind == kind).ToList();
        public Task SaveAsync(LearningRecord record) => record.Kind == kind ? parent.UpsertAsync(record) : throw new ArgumentException("Record kind mismatch.");
        public Task DeleteAsync(string courseId, string id) => parent.DeleteAsync(courseId, kind, id);
    }
}
