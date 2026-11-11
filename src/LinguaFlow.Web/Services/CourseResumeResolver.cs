using LinguaFlow.Core.Analytics;
using LinguaFlow.Core.Assessment;
using LinguaFlow.Core.Courses;

namespace LinguaFlow.Web.Services;

public sealed record CourseResume(UnitReference Unit, string Href, bool HasSavedPosition);

/// <summary>Turns stored course state into one safe learning destination without inventing progress.</summary>
public static class CourseResumeResolver
{
    public static CourseResume? Resolve(CourseManifest manifest, CourseAnalytics analytics, string? lastUnitId, string? lastSectionId)
    {
        var ordered = manifest.Units.OrderBy(x => x.Order).ToArray();
        if (ordered.Length == 0) return null;
        var remaining = ordered.Where(unit => !analytics.UnitStatuses.TryGetValue(unit.UnitId, out var status) || status != SkillStatus.Completed).ToArray();
        if (remaining.Length == 0) return null;
        var selected = remaining.FirstOrDefault(unit => unit.UnitId == analytics.CurrentUnitId) ?? remaining[0];
        var hasPosition = selected.UnitId == lastUnitId && !string.IsNullOrWhiteSpace(lastSectionId);
        var href = $"courses/{Uri.EscapeDataString(manifest.CourseId)}/units/{Uri.EscapeDataString(selected.UnitId)}";
        if (hasPosition) href += $"#{Uri.EscapeDataString(lastSectionId!)}";
        return new(selected, href, hasPosition);
    }
}
