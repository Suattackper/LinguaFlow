using System.Text.Json;

namespace LinguaFlow.Core.Progress;

public enum LearningRecordKind
{
    CourseProgress, UnitProgress, SectionProgress, ExerciseAttempt, WritingDraft,
    Bookmark, Note, VocabularyState, ReviewSchedule, StudySession, LearningGoal,
    AiPracticeSession, AiAssessmentResult, CourseSetting, UserPreference, Mistake
}

public sealed record LearningRecord
{
    public required string CourseId { get; init; }
    public required LearningRecordKind Kind { get; init; }
    public required string Id { get; init; }
    public string? UnitId { get; init; }
    public string? SectionId { get; init; }
    public string? ExerciseId { get; init; }
    public string? VocabularyId { get; init; }
    public required JsonElement Data { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required int RecordSchemaVersion { get; init; }
    public string StorageKey => $"{CourseId}|{Kind}|{Id}";
}

public sealed record CourseProgressBackup
{
    public required string Format { get; init; }
    public required int BackupSchemaVersion { get; init; }
    public required string CourseId { get; init; }
    public required string ContentVersion { get; init; }
    public required string ContentFingerprint { get; init; }
    public required DateTimeOffset ExportedAt { get; init; }
    public required List<LearningRecord> Progress { get; init; }
    public required List<LearningRecord> Attempts { get; init; }
    public required List<LearningRecord> ReviewStates { get; init; }
    public required List<LearningRecord> Notes { get; init; }
    public required List<LearningRecord> Bookmarks { get; init; }
    public required List<LearningRecord> StudySessions { get; init; }
    public required List<LearningRecord> AiPracticeResults { get; init; }
    public required List<LearningRecord> CourseSettings { get; init; }
    public required List<LearningRecord> Drafts { get; init; }
    public required List<LearningRecord> VocabularyStates { get; init; }
    public required List<LearningRecord> LearningGoals { get; init; }
    public List<LearningRecord> Mistakes { get; init; } = [];
    public IEnumerable<LearningRecord> AllRecords() => Progress.Concat(Attempts).Concat(ReviewStates).Concat(Notes).Concat(Bookmarks).Concat(StudySessions).Concat(AiPracticeResults).Concat(CourseSettings).Concat(Drafts).Concat(VocabularyStates).Concat(LearningGoals).Concat(Mistakes);
}
