using System.Text.Json;
using LinguaFlow.Core.Courses;
using LinguaFlow.Core.Practice;
using LinguaFlow.Core.Progress;
using LinguaFlow.Core.Analytics;

namespace LinguaFlow.Web.Services;

public sealed record PracticeImportPreview(PracticeSession Session, PracticeResult Result, PracticeEvaluation Evaluation, string ResultId, bool Duplicate);

public sealed class PracticeResultService(PracticeSessionService sessions, LearningRepository repository, PracticeResultValidator validator,
    CourseCatalogService catalog, CourseManifestService manifests, UnitContentService units, AssessmentContentService assessments)
{
    private readonly SemaphoreSlim _importGate = new(1, 1);
    public async Task<PracticeImportPreview> PreviewAsync(string courseId, string text, CancellationToken ct = default)
    {
        var requestId = PracticeResultValidator.ReadRequestId(text);
        if (requestId is null) throw new PracticeImportException("The JSON is invalid or has no requestId. Paste the complete result from ChatGPT.");
        var session = (await sessions.GetAsync(courseId)).FirstOrDefault(s => s.Request.RequestId == requestId)
            ?? throw new PracticeImportException("Original exported session not found on this device. Restore the course backup containing that session, then retry.");
        var entry = await catalog.FindAvailableAsync(courseId, ct) ?? throw new PracticeImportException("Course is unavailable.");
        var manifest = await manifests.GetManifestAsync(entry, ct);
        var unit = (await units.GetUnitAsync(entry, manifest, session.Request.UnitId, ct)).Content;
        if (!unit.Sections.SelectMany(s => s.Blocks).Any(b => b switch { SpeakingBlock s => s.ExerciseId == session.Request.TaskId, WritingBlock w => w.ExerciseId == session.Request.TaskId, ListeningBlock l => l.ExerciseId == session.Request.TaskId, _ => false })) throw new PracticeImportException("The original task is missing from this course version.");
        var rules = (await assessments.GetForUnitAsync(entry, manifest, unit, ct)).Thresholds;
        var rule = rules.FirstOrDefault(r => r.UnitId == unit.UnitId && r.Skill == session.Request.Skill && (r.MinimumPercent is not null || r.MinimumScore is not null));
        var validation = validator.Validate(text, session, manifest, rule);
        if (!validation.Valid) throw new PracticeImportException(string.Join(" ", validation.Errors));
        var id = PracticeResultValidator.ResultId(validation.Result!);
        var duplicate = (await repository.AiAssessmentResults.GetByCourseAsync(courseId)).Any(r => r.Id == id);
        return new(session, validation.Result!, validation.Evaluation!, id, duplicate);
    }
    public async Task<ImportedPracticeResult> ConfirmAsync(string courseId, string text, CancellationToken ct = default)
    {
        await _importGate.WaitAsync(ct);
        try
        {
            var preview = await PreviewAsync(courseId, text, ct);
            if (preview.Duplicate) throw new PracticeImportException("This result has already been imported.");
            var imported = new ImportedPracticeResult(preview.ResultId, preview.Result.RequestId, DateTimeOffset.UtcNow, preview.Result, preview.Evaluation);
            var updatedSession = preview.Session with { Status = PracticeSessionStatus.ResultImported, UpdatedAt = imported.ImportedAt };
            await repository.UpsertBatchAsync([
                new LearningRecord { CourseId = courseId, UnitId = preview.Result.UnitId, ExerciseId = preview.Result.TaskId, Kind = LearningRecordKind.AiAssessmentResult, Id = imported.ResultId, Data = JsonSerializer.SerializeToElement(imported), UpdatedAt = imported.ImportedAt, RecordSchemaVersion = 1 },
                new LearningRecord { CourseId = courseId, UnitId = preview.Result.UnitId, ExerciseId = preview.Result.TaskId, Kind = LearningRecordKind.AiPracticeSession, Id = preview.Session.Request.RequestId, Data = JsonSerializer.SerializeToElement(updatedSession), UpdatedAt = imported.ImportedAt, RecordSchemaVersion = 1 },
                StudyActivityService.Record(courseId, preview.Result.UnitId, StudyActivityKind.PracticeResultImported, imported.ImportedAt)
            ]);
            return imported;
        }
        finally { _importGate.Release(); }
    }
    public async Task<IReadOnlyList<ImportedPracticeResult>> GetResultsAsync(string courseId)
    {
        var records = await repository.AiAssessmentResults.GetByCourseAsync(courseId);
        return records.Select(r => r.Data.Deserialize<ImportedPracticeResult>(new JsonSerializerOptions(JsonSerializerDefaults.Web)))
            .Where(r => r is not null && r.Result.CourseId == courseId).Cast<ImportedPracticeResult>()
            .OrderByDescending(r => r.ImportedAt).ToList();
    }
    public async Task<ImportedPracticeResult?> FindAsync(string courseId, string resultId) => (await GetResultsAsync(courseId)).FirstOrDefault(r => r.ResultId == resultId);
}

public sealed class PracticeImportException(string message) : Exception(message);
