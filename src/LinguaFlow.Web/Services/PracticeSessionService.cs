using System.Text.Json;
using LinguaFlow.Core.Practice;
using LinguaFlow.Core.Progress;

namespace LinguaFlow.Web.Services;

public sealed class PracticeSessionService(LearningRepository repository)
{
    public async Task<IReadOnlyList<PracticeSession>> GetAsync(string courseId)
    {
        var records = await repository.AiPracticeSessions.GetByCourseAsync(courseId);
        return records.Select(r => r.Data.Deserialize<PracticeSession>(new JsonSerializerOptions(JsonSerializerDefaults.Web)))
            .Where(s => s is not null && s.Request.CourseId == courseId).Cast<PracticeSession>()
            .OrderByDescending(s => s.Request.GeneratedAt).ToList();
    }
    public Task SaveAsync(PracticeSession session)
    {
        if (session.Request.CourseId.Length == 0 || session.PackageMarkdown.Length == 0) throw new ArgumentException("Invalid session.");
        return repository.AiPracticeSessions.SaveAsync(new LearningRecord
        {
            CourseId = session.Request.CourseId, UnitId = session.Request.UnitId, ExerciseId = session.Request.TaskId,
            Kind = LearningRecordKind.AiPracticeSession, Id = session.Request.RequestId,
            Data = JsonSerializer.SerializeToElement(session), UpdatedAt = session.UpdatedAt, RecordSchemaVersion = 1
        });
    }
}
