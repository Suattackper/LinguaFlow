using System.Text.RegularExpressions;
using LinguaFlow.Core.Courses;

namespace LinguaFlow.CourseConverter;

public static class VocabularyParser
{
    public static VocabularyDocument Parse(string courseId, string markdown, List<string> warnings)
    {
        var entries = new List<VocabularyEntry>();
        var unitNumber = 0;
        var index = 0;
        foreach (var line in markdown.Split('\n'))
        {
            var heading = Regex.Match(line, @"^#{2,3}\s+Unit\s+(\d+)\s+[—–-]", RegexOptions.IgnoreCase);
            if (heading.Success) { unitNumber = int.Parse(heading.Groups[1].Value); index = 0; continue; }
            if (unitNumber == 0 || !line.TrimStart().StartsWith('|')) continue;
            var cells = line.Trim().Trim('|').Split('|').Select(x => x.Trim()).ToArray();
            if (cells.Length < 3 || cells[0] is "English" or "---" || cells[0].StartsWith('-')) continue;
            index++;
            entries.Add(new VocabularyEntry
            {
                VocabularyId = $"unit-{unitNumber:00}-vocab-{index:00}", UnitId = $"unit-{unitNumber:00}",
                Term = cells[0], Ipa = cells[1], Vietnamese = cells[2], Context = cells.Length >= 4 ? cells[3] : "",
                Source = new SourceProvenance { File = "VOCABULARY_BANK.md", OriginalUnit = $"{unitNumber:00}", OriginalSection = $"Unit {unitNumber:00}" }
            });
        }
        foreach (var group in entries.GroupBy(x => x.UnitId)) if (group.Count() != 20) warnings.Add($"VOCABULARY_BANK.md: {group.Key} has {group.Count()} entries, expected 20 from the source heading.");
        return new VocabularyDocument { SchemaVersion = 1, CourseId = courseId, Entries = entries };
    }
}
