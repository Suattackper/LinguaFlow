using LinguaFlow.Core.Courses;

namespace LinguaFlow.Web.Services;

public enum LessonStage { Learn, Practice, Test, Review }

/// <summary>Groups authored sections for navigation; never changes their content or order.</summary>
public static class LessonJourneyBuilder
{
    public static LessonStage StageFor(UnitSection section)
    {
        var id = section.SectionId.ToLowerInvariant();
        if (id == "h" || id.StartsWith("test-", StringComparison.Ordinal)) return LessonStage.Test;
        if (id == "i" || id.StartsWith("review-", StringComparison.Ordinal)) return LessonStage.Review;
        if (id is "c" or "d" or "e" or "f" or "g" or "reading-questions" ||
            id.StartsWith("c", StringComparison.Ordinal) || id.StartsWith("d", StringComparison.Ordinal) ||
            id.StartsWith("e", StringComparison.Ordinal) || id.StartsWith("f", StringComparison.Ordinal) ||
            id.StartsWith("g", StringComparison.Ordinal) ||
            section.Blocks.Any(b => b is QuestionBlock or WritingBlock or SpeakingBlock or ListeningBlock))
            return LessonStage.Practice;
        return LessonStage.Learn;
    }

    public static IReadOnlyList<LessonStage> Available(UnitContent unit) => unit.Sections
        .OrderBy(s => s.Order).Select(StageFor).Distinct().ToArray();

    public static UnitSection? First(UnitContent unit, LessonStage stage) => unit.Sections
        .OrderBy(s => s.Order).FirstOrDefault(s => StageFor(s) == stage);

    public static UnitSection? Next(UnitContent unit, string sectionId, int offset)
    {
        var ordered = unit.Sections.OrderBy(s => s.Order).ToArray();
        var index = Array.FindIndex(ordered, s => s.SectionId == sectionId);
        return index >= 0 && index + offset >= 0 && index + offset < ordered.Length ? ordered[index + offset] : null;
    }
}
