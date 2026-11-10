using System.Text.Json;
using LinguaFlow.Core.Assessment;
using LinguaFlow.Core.Progress;
using LinguaFlow.Core.Analytics;

namespace LinguaFlow.Web.Services;

public sealed class ExerciseSessionService(LearningRepository repository, ExerciseGradingService grading)
{
    private readonly SemaphoreSlim _submitLock = new(1, 1);
    public async Task<ExerciseAttempt> SubmitAsync(ExerciseDefinition definition, ExerciseResponse response, ExerciseMode mode)
    {
        await _submitLock.WaitAsync();
        try
        {
            var feedback = grading.Grade(definition, response);
            var attempt = new ExerciseAttempt
            {
                AttemptId = "attempt-" + Guid.NewGuid().ToString("N"), CourseId = definition.CourseId, UnitId = definition.UnitId,
                ExerciseId = definition.ExerciseId, SubmittedAt = DateTimeOffset.UtcNow, Mode = mode,
                Answers = response.Answers.ToArray(), Feedback = feedback
            };
            var records = new List<LearningRecord> { AttemptRecord(attempt), StudyActivityService.Record(definition.CourseId, definition.UnitId, StudyActivityKind.ExerciseSubmitted, attempt.SubmittedAt) };
            if (feedback.Status is GradingStatus.Incorrect or GradingStatus.Partial)
            {
                var mistake = new MistakeEntry
                {
                    CourseId = definition.CourseId, UnitId = definition.UnitId, ExerciseId = definition.ExerciseId,
                    LearnerAnswer = string.Join("; ", response.Answers), CorrectAnswer = feedback.CorrectAnswer ?? "",
                    Explanation = feedback.Explanation ?? "", Skill = definition.Skill, AttemptDate = attempt.SubmittedAt
                };
                records.Add(MistakeRecord(mistake));
            }
            await repository.UpsertBatchAsync(records);
            return attempt;
        }
        finally { _submitLock.Release(); }
    }
    public async Task<IReadOnlyList<ExerciseAttempt>> GetHistoryAsync(string courseId, string unitId, string exerciseId)
    {
        var records = await repository.ExerciseAttempts.GetByCourseAsync(courseId);
        return records.Where(x => x.UnitId == unitId && x.ExerciseId == exerciseId)
            .Select(x => x.Data.Deserialize<ExerciseAttempt>()).OfType<ExerciseAttempt>()
            .OrderByDescending(x => x.SubmittedAt).ToList();
    }
    public async Task<IReadOnlyList<ExerciseAttempt>> GetUnitAttemptsAsync(string courseId, string unitId)
    {
        var records = await repository.ExerciseAttempts.GetByCourseAsync(courseId);
        return records.Where(x => x.UnitId == unitId).Select(x => x.Data.Deserialize<ExerciseAttempt>()).OfType<ExerciseAttempt>().ToList();
    }
    public async Task<IReadOnlyList<MistakeEntry>> GetMistakesAsync(string courseId)
    {
        var records = await repository.Mistakes.GetByCourseAsync(courseId);
        return records.Select(x => x.Data.Deserialize<MistakeEntry>()).OfType<MistakeEntry>().OrderByDescending(x => x.AttemptDate).ToList();
    }
    public async Task MarkReviewedAsync(MistakeEntry mistake)
    {
        await repository.Mistakes.SaveAsync(MistakeRecord(mistake with { Reviewed = true }));
    }
    private static LearningRecord AttemptRecord(ExerciseAttempt attempt) => new()
    {
        CourseId = attempt.CourseId, UnitId = attempt.UnitId, ExerciseId = attempt.ExerciseId,
        Kind = LearningRecordKind.ExerciseAttempt, Id = attempt.AttemptId,
        Data = JsonSerializer.SerializeToElement(attempt), UpdatedAt = attempt.SubmittedAt, RecordSchemaVersion = 1
    };
    private static LearningRecord MistakeRecord(MistakeEntry mistake) => new()
    {
        CourseId = mistake.CourseId, UnitId = mistake.UnitId, ExerciseId = mistake.ExerciseId,
        Kind = LearningRecordKind.Mistake, Id = mistake.ExerciseId,
        Data = JsonSerializer.SerializeToElement(mistake), UpdatedAt = DateTimeOffset.UtcNow, RecordSchemaVersion = 1
    };
}
