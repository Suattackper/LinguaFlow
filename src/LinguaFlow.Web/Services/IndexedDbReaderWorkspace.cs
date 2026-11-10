using System.Text.Json;
using LinguaFlow.Core.Progress;

namespace LinguaFlow.Web.Services;

public sealed class IndexedDbReaderWorkspace(LearningRepository repository) : IReaderWorkspace
{
    private readonly SessionReaderWorkspace _cache = new();
    private readonly Dictionary<string, CancellationTokenSource> _pendingTokens = [];
    private readonly Dictionary<string, Task> _pendingTasks = [];
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly HashSet<string> _loaded = [];
    public string? StorageError { get; private set; }

    public async Task LoadCourseAsync(string courseId)
    {
        if (_loaded.Contains(courseId)) return;
        await _loadLock.WaitAsync();
        try
        {
            if (_loaded.Contains(courseId)) return;
            foreach (var record in (await repository.GetCourseAsync(courseId)).OrderBy(x => x.Kind == LearningRecordKind.CourseProgress ? 1 : 0))
            {
                var value = record.Data.ValueKind == JsonValueKind.String ? record.Data.GetString() ?? "" : "";
                switch (record.Kind)
                {
                    case LearningRecordKind.CourseProgress or LearningRecordKind.SectionProgress when record.UnitId is not null && record.SectionId is not null:
                        await _cache.SetPositionAsync(courseId, record.UnitId, record.SectionId); break;
                    case LearningRecordKind.WritingDraft when record.UnitId is not null && record.ExerciseId is not null:
                        await _cache.SetDraftAsync(courseId, record.UnitId, record.ExerciseId, value); break;
                    case LearningRecordKind.Note when record.UnitId is not null && record.SectionId is not null:
                        await _cache.SetNoteAsync(courseId, record.UnitId, record.SectionId, value); break;
                    case LearningRecordKind.Bookmark when record.UnitId is not null && record.SectionId is not null:
                        await _cache.SetBookmarkAsync(courseId, record.UnitId, record.SectionId, true); break;
                }
            }
            StorageError = null;
            _loaded.Add(courseId);
        }
        catch (LearningStorageException ex) { StorageError = ex.Message; }
        finally { _loadLock.Release(); }
    }
    public string? GetLastUnit(string courseId) => _cache.GetLastUnit(courseId);
    public string? GetLastSection(string courseId, string unitId) => _cache.GetLastSection(courseId, unitId);
    public string GetDraft(string courseId, string unitId, string exerciseId) => _cache.GetDraft(courseId, unitId, exerciseId);
    public bool IsBookmarked(string courseId, string unitId, string sectionId) => _cache.IsBookmarked(courseId, unitId, sectionId);
    public string GetNote(string courseId, string unitId, string sectionId) => _cache.GetNote(courseId, unitId, sectionId);

    public async Task SetPositionAsync(string courseId, string unitId, string sectionId)
    {
        await _cache.SetPositionAsync(courseId, unitId, sectionId);
        try
        {
            await repository.UpsertBatchAsync([
                Record(courseId, LearningRecordKind.SectionProgress, unitId, "", unitId, sectionId),
                Record(courseId, LearningRecordKind.CourseProgress, "position", "", unitId, sectionId)
            ]);
            StorageError = null;
        }
        catch (LearningStorageException ex) { StorageError = ex.Message; }
    }
    public async Task SetDraftAsync(string courseId, string unitId, string exerciseId, string text)
    {
        await _cache.SetDraftAsync(courseId, unitId, exerciseId, text);
        Debounce(Record(courseId, LearningRecordKind.WritingDraft, exerciseId, text, unitId, exerciseId: exerciseId));
    }
    public async Task SetBookmarkAsync(string courseId, string unitId, string sectionId, bool value)
    {
        await _cache.SetBookmarkAsync(courseId, unitId, sectionId, value);
        try
        {
            if (value) await repository.Bookmarks.SaveAsync(Record(courseId, LearningRecordKind.Bookmark, $"{unitId}-{sectionId}", "", unitId, sectionId));
            else await repository.Bookmarks.DeleteAsync(courseId, $"{unitId}-{sectionId}");
            StorageError = null;
        }
        catch (LearningStorageException ex) { StorageError = ex.Message; }
    }
    public async Task SetNoteAsync(string courseId, string unitId, string sectionId, string text)
    {
        await _cache.SetNoteAsync(courseId, unitId, sectionId, text);
        Debounce(Record(courseId, LearningRecordKind.Note, $"{unitId}-{sectionId}", text, unitId, sectionId));
    }
    private static LearningRecord Record(string course, LearningRecordKind kind, string id, string value, string? unit = null, string? section = null, string? exerciseId = null) => new()
    {
        CourseId = course, Kind = kind, Id = id, UnitId = unit, SectionId = section, ExerciseId = exerciseId,
        Data = JsonSerializer.SerializeToElement(value), UpdatedAt = DateTimeOffset.UtcNow, RecordSchemaVersion = 1
    };
    private async Task PersistAsync(LearningRecord record)
    {
        try { await repository.UpsertAsync(record); StorageError = null; }
        catch (LearningStorageException ex) { StorageError = ex.Message; }
    }
    private void Debounce(LearningRecord record)
    {
        var key = record.StorageKey;
        if (_pendingTokens.Remove(key, out var old)) { old.Cancel(); old.Dispose(); }
        var cts = new CancellationTokenSource();
        _pendingTokens[key] = cts;
        _pendingTasks[key] = SaveLaterAsync(record, cts.Token);
    }
    private async Task SaveLaterAsync(LearningRecord record, CancellationToken token)
    {
        try { await Task.Delay(350, token); await PersistAsync(record); }
        catch (OperationCanceledException) { }
    }
    public async Task FlushAsync()
    {
        await Task.WhenAll(_pendingTasks.Values.ToArray());
        _pendingTasks.Clear();
    }
    public void InvalidateCache() { _cache.InvalidateCache(); _loaded.Clear(); }
}
