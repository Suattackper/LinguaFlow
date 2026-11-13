using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LinguaFlow.Core.Courses;

namespace LinguaFlow.Core.Practice;

public enum PracticeVerdict { Passed, NeedsImprovement, NotAssessed, InsufficientEvidence, InvalidResult }
public sealed record PracticeEvaluation(int EarnedPoints, int MaximumPoints, decimal Percentage, PracticeVerdict Verdict, bool EvidenceComplete, bool RequiredCriteriaSatisfied, List<string> Warnings, string PassRule);
public sealed record ImportedPracticeResult(string ResultId, string RequestId, DateTimeOffset ImportedAt, PracticeResult Result, PracticeEvaluation Evaluation, int ImportSchemaVersion = 1);
public sealed record PracticeValidation(PracticeResult? Result, PracticeEvaluation? Evaluation, IReadOnlyList<string> Errors)
{
    public bool Valid => Result is not null && Evaluation is not null && Errors.Count == 0;
}

public sealed class PracticeResultValidator
{
    public const int MaxBytes = 64 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { MaxDepth = 32 };

    public static string? ReadRequestId(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || Encoding.UTF8.GetByteCount(input) > MaxBytes) return null;
        var json = StripFence(input.Trim());
        if (json is null) return null;
        try
        {
            using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 32 });
            return doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("requestId", out var id) && id.ValueKind == JsonValueKind.String ? id.GetString() : null;
        }
        catch (JsonException) { return null; }
    }

    public PracticeValidation Validate(string input, PracticeSession session, CourseManifest manifest, ThresholdRule? passRule = null)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(input) || Encoding.UTF8.GetByteCount(input) > MaxBytes) return new(null, null, ["Result JSON is empty or exceeds 64 KiB."]);
        var json = StripFence(input.Trim());
        if (json is null) return new(null, null, ["Paste one JSON object or one fenced JSON code block."]);
        PracticeResult? result;
        try
        {
            using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 32, AllowTrailingCommas = false, CommentHandling = JsonCommentHandling.Disallow });
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return new(null, null, ["The result must be a JSON object."]);
            result = doc.RootElement.Deserialize<PracticeResult>(JsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException) { return new(null, null, ["Invalid JSON or missing required result fields."]); }
        if (result is null) return new(null, null, ["Result JSON is empty."]);
        var request = session.Request;
        if (result.Format != "linguaflow-practice-result") errors.Add("Wrong result format.");
        if (result.SchemaVersion != 1 || request.ExpectedResultSchemaVersion != 1) errors.Add("Unsupported result schema version.");
        if (result.RequestId != request.RequestId) errors.Add("Request ID does not match the exported session.");
        if (result.CourseId != request.CourseId || result.CourseId != manifest.CourseId) errors.Add("Course ID does not match.");
        if (result.CourseContentVersion != request.CourseContentVersion || !CourseContentCompatibility.Accepts(manifest, request.CourseContentVersion, request.CourseContentFingerprint)) errors.Add("Course content version or fingerprint differs; no migration is defined.");
        if (result.UnitId != request.UnitId) errors.Add("Unit ID does not match.");
        if (result.TaskId != request.TaskId) errors.Add("Task ID does not match.");
        if (result.Skill != request.Skill) errors.Add("Skill does not match.");
        if (result.RubricId != request.RubricId || result.RubricVersion != request.RubricVersion) errors.Add("Rubric ID or version does not match.");
        if (result.CompletedAt == default || result.CompletedAt < request.GeneratedAt.AddMinutes(-5) || result.CompletedAt > DateTimeOffset.UtcNow.AddDays(1)) errors.Add("Completion date is invalid.");
        if (result.Evaluator is null || !result.Evaluator.StartsWith("external-ai:", StringComparison.OrdinalIgnoreCase) || result.Evaluator.Length > 120) errors.Add("Evaluator must identify an external AI.");
        if (result.Evidence is null || result.Evidence.Modality is not ("text-only" or "audio-observed" or "audio-not-observed" or "unknown") || !ValidText(result.Evidence.Description, 1000, true)) errors.Add("Evidence modality or description is invalid.");
        if (!ValidText(result.Summary, 4000, true) || !ValidList(result.Strengths, 20, 2000) || !ValidList(result.VocabularySuggestions, 20, 1000) || !ValidList(result.ImprovementSuggestions, 20, 2000) || result.Mistakes is null || result.Mistakes.Count > 30 || result.Mistakes.Any(m => m is null || !ValidText(m.Original, 2000) || !ValidText(m.Correction, 2000) || !ValidText(m.Explanation, 2000))) errors.Add("Feedback fields are missing or too long.");
        if (result.CriterionAssessments is null || result.CriterionAssessments.Count != request.AssessmentCriteria.Count || result.CriterionAssessments.Any(c => c is null)) errors.Add("Criterion assessment count is incorrect.");
        else
        {
            var expected = request.AssessmentCriteria.ToDictionary(c => c.CriterionId);
            if (expected.Count != request.AssessmentCriteria.Count || result.CriterionAssessments.Select(c => c.CriterionId).Distinct(StringComparer.Ordinal).Count() != result.CriterionAssessments.Count) errors.Add("Duplicate criterion IDs.");
            foreach (var criterion in result.CriterionAssessments)
            {
                if (!expected.TryGetValue(criterion.CriterionId ?? "", out var authored)) { errors.Add("Unknown criterion ID."); continue; }
                if (criterion.MaxPoints != authored.MaxPoints || criterion.AwardedPoints is < 0 || criterion.AwardedPoints > authored.MaxPoints) errors.Add($"Invalid points for {authored.CriterionId}.");
                if (criterion.EvidenceLevel is not ("sufficient" or "partial" or "insufficient") || !ValidText(criterion.EvidenceSummary, 2000, true)) errors.Add($"Invalid evidence for {authored.CriterionId}.");
                if ((criterion.EvidenceLevel == "insufficient") != (criterion.AwardedPoints is null)) errors.Add($"Score and evidence level conflict for {authored.CriterionId}.");
                if (RequiresAudio(authored) && result.Evidence?.Modality != "audio-observed" && criterion.AwardedPoints is not null) errors.Add($"Audio evidence is required for {authored.CriterionId}.");
            }
        }
        if (session.Status == PracticeSessionStatus.Cancelled) errors.Add("The original session was cancelled.");
        if (errors.Count > 0) return new(result, null, errors);
        var scores = result.CriterionAssessments!;
        var warnings = new List<string>();
        if (result.Evidence!.Modality != "audio-observed" && (request.Skill is "speaking" or "listening")) warnings.Add("Pronunciation is NOT ASSESSED without suitable observed audio.");
        if (request.Skill == "listening" && result.Evidence.Modality != "audio-observed") warnings.Add("Listening remains unverified because audio was not observed.");
        var incomplete = scores.Any(c => c.AwardedPoints is null || c.EvidenceLevel == "insufficient");
        if (incomplete) warnings.Add("At least one rubric criterion lacks enough evidence.");
        var earned = scores.Sum(c => c.AwardedPoints ?? 0);
        var maximum = request.AssessmentCriteria.Sum(c => c.MaxPoints);
        var percent = maximum == 0 ? 0 : Math.Round((decimal)earned / maximum * 100, 1);
        var ruleApplies = passRule is not null && passRule.UnitId == request.UnitId && passRule.Skill == request.Skill;
        var minimum = ruleApplies ? passRule!.MinimumPercent ?? (passRule.MinimumScore is { } min && passRule.TotalScore is { } total && total > 0 ? (int)Math.Ceiling((decimal)min / total * 100) : null) : null;
        var verdict = incomplete || (request.Skill == "listening" && result.Evidence.Modality != "audio-observed") ? PracticeVerdict.InsufficientEvidence
            : minimum is null ? PracticeVerdict.NotAssessed
            : percent >= minimum ? PracticeVerdict.Passed : PracticeVerdict.NeedsImprovement;
        if (minimum is null) warnings.Add("The course has no numeric pass rule for this skill; the rubric score is feedback only.");
        return new(result, new(earned, maximum, percent, verdict, !incomplete && (request.Skill != "listening" || result.Evidence.Modality == "audio-observed"), !incomplete, warnings, minimum is null ? "No authored numeric pass rule" : passRule!.Text), []);
    }

    public static string ResultId(PracticeResult result)
    {
        var canonical = JsonSerializer.Serialize(result, JsonOptions);
        return "result-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))[..32].ToLowerInvariant();
    }
    private static bool RequiresAudio(PracticeCriterion c) => c.EvidenceRequirement.Contains("audio", StringComparison.OrdinalIgnoreCase) || c.Title.Contains("pronunciation", StringComparison.OrdinalIgnoreCase) || c.Title.Contains("phát âm", StringComparison.OrdinalIgnoreCase) || c.Title.Contains("listening", StringComparison.OrdinalIgnoreCase) || c.Title.Contains("nghe", StringComparison.OrdinalIgnoreCase);
    private static bool ValidText(string? text, int max, bool required = false) => text is not null && text.Length <= max && (!required || !string.IsNullOrWhiteSpace(text)) && !text.Any(ch => char.IsControl(ch) && ch is not ('\n' or '\r' or '\t'));
    private static bool ValidList(List<string>? list, int maxItems, int maxLength) => list is not null && list.Count <= maxItems && list.All(x => ValidText(x, maxLength));
    private static string? StripFence(string input)
    {
        if (!input.StartsWith("```", StringComparison.Ordinal)) return input.StartsWith('{') && input.EndsWith('}') ? input : null;
        var newline = input.IndexOf('\n');
        if (newline < 0 || input[..newline].Trim() is not ("```json" or "```JSON" or "```")) return null;
        var end = input.LastIndexOf("```", StringComparison.Ordinal);
        return end > newline && input[(end + 3)..].Trim().Length == 0 ? input[(newline + 1)..end].Trim() : null;
    }
}
