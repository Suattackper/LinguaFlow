using System.Text.Json;
using LinguaFlow.Core.Courses;
using LinguaFlow.Core.Progress;
using LinguaFlow.Core.Vocabulary;
using LinguaFlow.Core.Analytics;

namespace LinguaFlow.Web.Services;

public sealed class VocabularyLearningService(LearningRepository repository, SpacedRepetitionService spacing)
{
    public async Task<Dictionary<string, VocabularyReviewState>> GetStatesAsync(string courseId)
    {
        var records = await repository.VocabularyStates.GetByCourseAsync(courseId);
        var result = new Dictionary<string, VocabularyReviewState>(StringComparer.Ordinal);
        foreach (var record in records)
        {
            VocabularyReviewState? state;
            try { state = record.Data.Deserialize<VocabularyReviewState>(); }
            catch (JsonException ex) { throw new CourseContentException($"Vocabulary state {record.Id} is invalid.", ex); }
            if (state is null || state.CourseId != courseId || state.VocabularyId != record.Id || state.UnitId != record.UnitId || state.ReviewStage < 0 || state.ReviewCount < 0 || state.History is null) throw new CourseContentException($"Vocabulary state {record.Id} is inconsistent.");
            if (!result.TryAdd(record.Id, state)) throw new CourseContentException($"Duplicate vocabulary state: {record.Id}.");
        }
        return result;
    }
    public static VocabularyReviewState NewState(string courseId, VocabularyEntry word) => new() { CourseId = courseId, UnitId = word.UnitId, VocabularyId = word.VocabularyId };
    public async Task<VocabularyReviewState> ReviewAsync(string courseId, VocabularyEntry word, VocabularyReviewState? prior, bool remembered, IReadOnlyList<int>? unitIntervals, DateTimeOffset now)
    {
        var state = prior ?? NewState(courseId, word);
        Validate(courseId, word, state);
        var next = spacing.Review(state, remembered, now, unitIntervals);
        await repository.UpsertBatchAsync([StateRecord(next), StudyActivityService.Record(courseId, word.UnitId, StudyActivityKind.VocabularyReviewed, now)]);
        return next;
    }
    public async Task<VocabularyReviewState> UpdateAsync(string courseId, VocabularyEntry word, VocabularyReviewState? prior, bool? favorite = null, bool? inQueue = null, string? note = null)
    {
        var state = prior ?? NewState(courseId, word);
        Validate(courseId, word, state);
        var next = state with { Favorite = favorite ?? state.Favorite, InReviewQueue = inQueue ?? state.InReviewQueue, Note = note ?? state.Note };
        await SaveAsync(next);
        return next;
    }
    private static void Validate(string courseId, VocabularyEntry word, VocabularyReviewState state)
    {
        if (state.CourseId != courseId || state.UnitId != word.UnitId || state.VocabularyId != word.VocabularyId) throw new ArgumentException("Vocabulary state belongs to a different course or word.");
    }
    private Task SaveAsync(VocabularyReviewState state) => repository.VocabularyStates.SaveAsync(StateRecord(state));
    private static LearningRecord StateRecord(VocabularyReviewState state) => new()
    {
        CourseId = state.CourseId, UnitId = state.UnitId, VocabularyId = state.VocabularyId,
        Kind = LearningRecordKind.VocabularyState, Id = state.VocabularyId,
        Data = JsonSerializer.SerializeToElement(state), UpdatedAt = DateTimeOffset.UtcNow, RecordSchemaVersion = 1
    };
}
