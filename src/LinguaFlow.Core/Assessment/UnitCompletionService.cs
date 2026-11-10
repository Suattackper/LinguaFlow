using LinguaFlow.Core.Courses;
using LinguaFlow.Core.Practice;

namespace LinguaFlow.Core.Assessment;

public sealed class UnitCompletionService
{
    public UnitAssessment Evaluate(string unitId, IReadOnlyList<ThresholdRule> rules, IReadOnlyList<ExerciseAttempt> attempts, IReadOnlyList<ImportedPracticeResult>? practiceResults = null)
    {
        var practice = (practiceResults ?? []).Where(x => x.Result.UnitId == unitId).ToList();
        var relevant = rules.Where(x => x.UnitId == unitId).ToList();
        var latest = attempts.Where(x => x.UnitId == unitId).GroupBy(x => x.ExerciseId).Select(x => x.OrderByDescending(a => a.SubmittedAt).First()).ToList();
        var skills = new List<SkillAssessment>();
        var remaining = new List<string>();
        foreach (var rule in relevant)
        {
            if (rule.Skill == "progression")
            {
                var related = new[] { "writing", "speaking", "listening" }.FirstOrDefault(s => rule.Text.Contains(s, StringComparison.OrdinalIgnoreCase));
                if (related is not null && practice.Any(p => p.Result.Skill == related && p.Evaluation.Verdict == PracticeVerdict.Passed)) continue;
                if (related is not null || rule.Text.Contains("đánh giá tổng hợp", StringComparison.OrdinalIgnoreCase)) remaining.Add(rule.Text);
                continue;
            }
            var skillAttempts = latest.Where(a => SkillFromId(a.ExerciseId) == rule.Skill).ToList();
            var earned = skillAttempts.Sum(a => a.Feedback.EarnedScore);
            var possible = skillAttempts.Sum(a => a.Feedback.MaximumScore);
            var pending = skillAttempts.Any(a => a.Feedback.Status is GradingStatus.NeedsReview or GradingStatus.InsufficientEvidence);
            SkillStatus status;
            if (skillAttempts.Count == 0) status = SkillStatus.NotStarted;
            else if (pending) status = SkillStatus.NeedsReview;
            else if (rule.MinimumScore is { } minimum && rule.TotalScore is { } total && possible >= total) status = earned >= minimum ? SkillStatus.Passed : SkillStatus.InProgress;
            else if (rule.MinimumPercent is { } percent && possible > 0 && rule.TotalScore is { } requiredTotal && possible >= requiredTotal) status = earned / possible * 100 >= percent ? SkillStatus.Passed : SkillStatus.InProgress;
            else status = SkillStatus.InProgress;
            skills.Add(new SkillAssessment(rule.Skill, status, earned, possible, rule.Text));
            if (status != SkillStatus.Passed) remaining.Add(rule.Text);
        }
        foreach (var openSkill in new[] { "writing", "speaking", "listening" })
        {
            var imported = practice.Where(p => p.Result.Skill == openSkill).ToList();
            if (imported.Any(p => p.Evaluation.Verdict == PracticeVerdict.Passed)) { skills.Add(new SkillAssessment(openSkill, SkillStatus.Passed, imported.Where(p => p.Evaluation.Verdict == PracticeVerdict.Passed).Max(p => p.Evaluation.EarnedPoints), 100, "External AI assessment; rubric and evidence validated, conversation not independently verified.")); continue; }
            var openAttempts = latest.Where(a => a.ExerciseId.EndsWith("-" + openSkill, StringComparison.OrdinalIgnoreCase)).ToList();
            if (imported.Count > 0) { var best = imported.OrderByDescending(p => p.Evaluation.EarnedPoints).First(); skills.Add(new SkillAssessment(openSkill, best.Evaluation.Verdict == PracticeVerdict.InsufficientEvidence ? SkillStatus.InsufficientEvidence : SkillStatus.NeedsReview, best.Evaluation.EarnedPoints, best.Evaluation.MaximumPoints, "External AI assessment; no verified course pass for this skill.")); continue; }
            if (openAttempts.Count == 0) { skills.Add(new SkillAssessment(openSkill, SkillStatus.NotStarted, 0, 0, "")); continue; }
            var status = openAttempts.Any(a => a.Feedback.Status == GradingStatus.InsufficientEvidence) ? SkillStatus.InsufficientEvidence : SkillStatus.Submitted;
            skills.Add(new SkillAssessment(openSkill, status, 0, 0, ""));
        }
        var complete = relevant.Count > 0 && skills.Count > 0 && skills.All(x => x.Status == SkillStatus.Passed) && remaining.Count == 0;
        var overall = complete ? SkillStatus.Completed : latest.Count == 0 && practice.Count == 0 ? SkillStatus.NotStarted : remaining.Count > 0 ? SkillStatus.NeedsReview : SkillStatus.InProgress;
        return new UnitAssessment(unitId, overall, skills, remaining);
    }

    private static string SkillFromId(string id)
    {
        var parts = id.Split('-');
        if (parts.Length < 3) return "other";
        var section = parts[2];
        return section.StartsWith('c') ? "grammar" : section.StartsWith('d') ? "vocabulary" : section.StartsWith('e') || section == "reading" ? "reading" : "other";
    }
}
