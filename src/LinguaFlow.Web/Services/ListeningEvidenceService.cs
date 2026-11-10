using System.Text.Json;
using LinguaFlow.Core.Progress;

namespace LinguaFlow.Web.Services;

public sealed record ListeningPlaybackEvidence(string CourseId, string UnitId, string BlockId, DateTimeOffset StartedAt, string Source);

public sealed class ListeningEvidenceService(LearningRepository repository)
{
    public Task RecordAsync(string courseId, string unitId, string blockId, string source)
    {
        var now = DateTimeOffset.UtcNow;
        var evidence = new ListeningPlaybackEvidence(courseId, unitId, blockId, now, source);
        return repository.StudySessions.SaveAsync(new LearningRecord
        {
            CourseId = courseId, UnitId = unitId, Kind = LearningRecordKind.StudySession,
            Id = "playback-" + Guid.NewGuid().ToString("N"), Data = JsonSerializer.SerializeToElement(evidence),
            UpdatedAt = now, RecordSchemaVersion = 1
        });
    }
    public async Task<bool> HasPlaybackAsync(string courseId, string unitId)
    {
        var rows = await repository.StudySessions.GetByCourseAsync(courseId);
        return rows.Any(x => x.UnitId == unitId && x.Id.StartsWith("playback-", StringComparison.Ordinal));
    }
}
