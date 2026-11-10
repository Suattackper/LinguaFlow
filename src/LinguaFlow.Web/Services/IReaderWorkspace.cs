namespace LinguaFlow.Web.Services;

// Synchronous reads come from a hydrated per-course cache; writes are awaited by UI handlers.
public interface IReaderWorkspace
{
    Task LoadCourseAsync(string courseId);
    string? GetLastUnit(string courseId);
    string? GetLastSection(string courseId, string unitId);
    Task SetPositionAsync(string courseId, string unitId, string sectionId);
    string GetDraft(string courseId, string unitId, string exerciseId);
    Task SetDraftAsync(string courseId, string unitId, string exerciseId, string text);
    bool IsBookmarked(string courseId, string unitId, string sectionId);
    Task SetBookmarkAsync(string courseId, string unitId, string sectionId, bool value);
    string GetNote(string courseId, string unitId, string sectionId);
    Task SetNoteAsync(string courseId, string unitId, string sectionId, string text);
    Task FlushAsync();
    void InvalidateCache();
    string? StorageError { get; }
}

public sealed class SessionReaderWorkspace : IReaderWorkspace
{
    private readonly Dictionary<string, string> _positions = [];
    private readonly Dictionary<string, string> _drafts = [];
    private readonly Dictionary<string, string> _notes = [];
    private readonly HashSet<string> _bookmarks = [];
    private static string Key(params string[] parts) => string.Join(':', parts);
    public string? StorageError => null;
    public Task LoadCourseAsync(string courseId) => Task.CompletedTask;
    public string? GetLastUnit(string courseId) => _positions.GetValueOrDefault(courseId);
    public string? GetLastSection(string courseId, string unitId) => _positions.GetValueOrDefault(Key(courseId, unitId));
    public Task SetPositionAsync(string courseId, string unitId, string sectionId) { _positions[courseId] = unitId; _positions[Key(courseId, unitId)] = sectionId; return Task.CompletedTask; }
    public string GetDraft(string courseId, string unitId, string exerciseId) => _drafts.GetValueOrDefault(Key(courseId, unitId, exerciseId)) ?? "";
    public Task SetDraftAsync(string courseId, string unitId, string exerciseId, string text) { _drafts[Key(courseId, unitId, exerciseId)] = text; return Task.CompletedTask; }
    public bool IsBookmarked(string courseId, string unitId, string sectionId) => _bookmarks.Contains(Key(courseId, unitId, sectionId));
    public Task SetBookmarkAsync(string courseId, string unitId, string sectionId, bool value) { var key = Key(courseId, unitId, sectionId); if (value) _bookmarks.Add(key); else _bookmarks.Remove(key); return Task.CompletedTask; }
    public string GetNote(string courseId, string unitId, string sectionId) => _notes.GetValueOrDefault(Key(courseId, unitId, sectionId)) ?? "";
    public Task SetNoteAsync(string courseId, string unitId, string sectionId, string text) { _notes[Key(courseId, unitId, sectionId)] = text; return Task.CompletedTask; }
    public Task FlushAsync() => Task.CompletedTask;
    public void InvalidateCache() { _positions.Clear(); _drafts.Clear(); _notes.Clear(); _bookmarks.Clear(); }
}
