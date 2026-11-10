using System.Text.RegularExpressions;

namespace LinguaFlow.CourseConverter;

public sealed record MarkdownSection(int Level, string Heading, string Body, string Raw, int Order);

public static partial class MarkdownStructure
{
    [GeneratedRegex(@"(?m)^(#{1,3})\s+(.+?)\s*$")]
    private static partial Regex HeadingPattern();

    public static IReadOnlyList<MarkdownSection> Split(string markdown)
    {
        var matches = HeadingPattern().Matches(markdown);
        var result = new List<MarkdownSection>();
        for (var index = 0; index < matches.Count; index++)
        {
            var match = matches[index];
            var end = index + 1 < matches.Count ? matches[index + 1].Index : markdown.Length;
            var raw = markdown[match.Index..end].Trim();
            var body = markdown[(match.Index + match.Length)..end].Trim();
            result.Add(new MarkdownSection(match.Groups[1].Length, match.Groups[2].Value.Trim(), body, raw, index + 1));
        }
        return result;
    }

    public static string Plain(string markdown) => markdown.Replace("**", "").Replace("`", "").Trim();

    public static string StableSectionId(string heading)
    {
        var code = Regex.Match(heading, @"^([A-I](?:[1-9])?)\.", RegexOptions.IgnoreCase);
        if (code.Success) return code.Groups[1].Value.ToLowerInvariant();
        if (heading.Contains("objective", StringComparison.OrdinalIgnoreCase) || heading.Contains("Mục tiêu", StringComparison.OrdinalIgnoreCase)) return "objectives";
        if (heading.Contains("learning plan", StringComparison.OrdinalIgnoreCase) || heading.Contains("Kế hoạch", StringComparison.OrdinalIgnoreCase)) return "learning-plan";
        if (heading.Contains("Reading questions", StringComparison.OrdinalIgnoreCase)) return "reading-questions";
        var slug = Regex.Replace(heading.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        return slug.Length == 0 ? "section" : slug;
    }

    public static IReadOnlyList<(int Number, string Prompt, List<string> Options)> NumberedQuestions(string body)
    {
        var questions = new List<(int Number, string Prompt, List<string> Options)>();
        foreach (var line in body.Split('\n'))
        {
            var numbered = Regex.Match(line, @"^\s*(\d+)\.\s+(.+)$");
            if (numbered.Success)
            {
                questions.Add((int.Parse(numbered.Groups[1].Value), numbered.Groups[2].Value.Trim(), []));
                continue;
            }
            var option = Regex.Match(line, @"^\s*-\s+([A-C])\.\s+(.+)$");
            if (option.Success && questions.Count > 0) questions[^1].Options.Add($"{option.Groups[1].Value}. {option.Groups[2].Value.Trim()}");
        }
        return questions;
    }
}
