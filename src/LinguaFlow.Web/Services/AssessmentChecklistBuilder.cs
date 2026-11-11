using LinguaFlow.Core.Assessment;
using LinguaFlow.Core.Courses;

namespace LinguaFlow.Web.Services;

public sealed record AssessmentChecklistItem(string Skill, string Text, SkillStatus Status, string? SectionId,
    int? MinimumScore, int? TotalScore, int? MinimumPercent, bool IsProgression);

/// <summary>Maps authored rules to existing sections without changing course completion decisions.</summary>
public static class AssessmentChecklistBuilder
{
    public static IReadOnlyList<AssessmentChecklistItem> Build(UnitContent unit, IReadOnlyList<ThresholdRule> rules, UnitAssessment assessment)
    {
        var result = new List<AssessmentChecklistItem>();
        var used = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in rules.Where(x => x.UnitId == unit.UnitId))
        {
            if (rule.Skill == "progression")
            {
                if (!assessment.RemainingRequirements.Contains(rule.Text)) continue;
                var related = RuleSkills(rule.Text).ToArray();
                foreach (var skill in related.Length == 0 ? ["progression"] : related)
                    result.Add(new(skill, rule.Text, SkillStatus.NeedsReview, FindSection(unit, skill),
                        rule.MinimumScore, rule.TotalScore, rule.MinimumPercent, true));
                continue;
            }
            var index = used.GetValueOrDefault(rule.Skill);
            used[rule.Skill] = index + 1;
            var status = assessment.Skills.Where(x => x.Skill == rule.Skill).Skip(index).FirstOrDefault()?.Status ?? SkillStatus.NotStarted;
            result.Add(new(rule.Skill, rule.Text, status, FindSection(unit, rule.Skill),
                rule.MinimumScore, rule.TotalScore, rule.MinimumPercent, false));
        }
        return result;
    }

    private static IEnumerable<string> RuleSkills(string text)
    {
        foreach (var skill in new[] { "writing", "speaking", "listening" })
            if (text.Contains(skill, StringComparison.OrdinalIgnoreCase)) yield return skill;
    }

    private static string? FindSection(UnitContent unit, string? skill)
    {
        if (skill is null) return null;
        return unit.Sections.OrderBy(x => x.Order).FirstOrDefault(section => skill switch
        {
            "grammar" => section.SectionId.StartsWith('c') && section.Blocks.OfType<QuestionBlock>().Any(),
            "vocabulary" => section.SectionId.StartsWith('d') && section.Blocks.OfType<QuestionBlock>().Any(),
            "reading" => (section.SectionId.StartsWith('e') || section.SectionId.Contains("reading", StringComparison.OrdinalIgnoreCase)) && section.Blocks.OfType<QuestionBlock>().Any(),
            "writing" => section.Blocks.OfType<WritingBlock>().Any(),
            "speaking" => section.Blocks.OfType<SpeakingBlock>().Any(),
            "listening" => section.Blocks.OfType<ListeningBlock>().Any(),
            _ => false
        })?.SectionId;
    }
}
