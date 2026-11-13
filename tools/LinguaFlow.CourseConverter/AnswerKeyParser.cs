using System.Text.RegularExpressions;
using LinguaFlow.Core.Courses;

namespace LinguaFlow.CourseConverter;

public sealed record ParsedAnswers(IReadOnlyList<AnswerRecord> Answers, IReadOnlyList<RubricRecord> Rubrics, IReadOnlyList<string> Warnings);

public static class AnswerKeyParser
{
    public static ParsedAnswers Parse(int number, string markdown, UnitContent unit, string? sourceFileOverride = null)
    {
        var sourceFile = sourceFileOverride ?? $"ANSWER_KEYS/Unit_{number:00}_Answers.md";
        var headings = MarkdownStructure.Split(markdown);
        var title = headings.FirstOrDefault()?.Heading ?? "";
        if (!Regex.IsMatch(title, $@"^ANSWER KEY\s+[—–-]\s+UNIT\s+{number:00}:", RegexOptions.IgnoreCase))
            throw new InvalidOperationException($"{sourceFile}: answer key Unit heading does not match its filename.");
        var answers = new List<AnswerRecord>();
        var rubrics = new List<RubricRecord>();
        var warnings = new List<string>();
        var questions = unit.Sections.SelectMany(s => s.Blocks).OfType<QuestionBlock>().ToDictionary(x => x.ExerciseId);
        foreach (var section in headings.Where(x => x.Level == 2))
        {
            var code = MarkdownStructure.StableSectionId(section.Heading);
            var group = code == "e" ? "e" : code;
            var source = new SourceProvenance { File = sourceFile, OriginalUnit = $"{number:00}", OriginalSection = section.Heading };
            if (group is "f" or "g")
            {
                rubrics.Add(new RubricRecord { RubricId = $"unit-{number:00}-{group}-rubric", UnitId = unit.UnitId, Skill = group == "f" ? "writing" : "speaking-listening", Text = section.Body, Source = source });
                continue;
            }
            if (group is not ("c1" or "c2" or "c3" or "d" or "d1" or "e")) continue;
            var rows = group == "e" ? ReadingAnswers(section.Body) : TableAnswers(section.Body);
            foreach (var (questionNumber, answer, explanation) in rows)
            {
                var id = $"unit-{number:00}-{group}-{questionNumber:00}";
                if (!questions.TryGetValue(id, out var question))
                {
                    warnings.Add($"{sourceFile}: answer {id} has no lesson exercise.");
                    continue;
                }
                var multipleBlanks = number == 1 && group == "c1" && question.ResponseKind == "multiple-blanks";
                var ambiguous = answer.Contains(" / ", StringComparison.Ordinal) || (answer.Contains(';') && !multipleBlanks);
                var mode = question.ResponseKind == "multiple-choice" && Regex.IsMatch(answer, "^[A-C]$") ? "auto-choice"
                    : number == 1 && !ambiguous && (group is "c1" or "d") ? "auto-exact" : "manual-review";
                if (ambiguous) warnings.Add($"{sourceFile}: {id} has alternatives or multiple blanks; manual review required.");
                List<string>? validAnswers = null;
                if (multipleBlanks) validAnswers = answer.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
                else if (number == 1 && group == "c1" && questionNumber == 3 && answer == "do not work") validAnswers = [answer, "don't work"];
                else if (number == 1 && group == "c1" && questionNumber == 12 && answer == "does not understand") validAnswers = [answer, "doesn't understand"];
                answers.Add(new AnswerRecord
                {
                    ExerciseId = id, UnitId = unit.UnitId, Answer = answer, Explanation = group is "d" or "d1" ? "" : explanation,
                    AssessmentMode = mode, Ambiguous = ambiguous, ValidAnswers = validAnswers,
                    IgnorePunctuation = number == 1 && mode == "auto-exact", Source = source
                });
            }
        }
        var matched = answers.Select(x => x.ExerciseId).ToHashSet(StringComparer.Ordinal);
        foreach (var question in questions.Values.Where(x => !matched.Contains(x.ExerciseId))) warnings.Add($"{sourceFile}: missing answer for {question.ExerciseId}.");
        return new ParsedAnswers(answers, rubrics, warnings);
    }

    private static IReadOnlyList<(int Number, string Answer, string Explanation)> TableAnswers(string body)
    {
        var result = new List<(int, string, string)>();
        foreach (var line in body.Split('\n'))
        {
            if (!line.TrimStart().StartsWith('|')) continue;
            var cells = line.Trim().Trim('|').Split('|').Select(x => MarkdownStructure.Plain(x)).ToArray();
            if (cells.Length < 2 || !int.TryParse(cells[0], out var number)) continue;
            result.Add((number, cells[1], cells.Length >= 3 ? cells[2] : ""));
        }
        return result;
    }

    private static IReadOnlyList<(int Number, string Answer, string Explanation)> ReadingAnswers(string body)
    {
        var result = new List<(int, string, string)>();
        foreach (var line in body.Split('\n'))
        {
            var match = Regex.Match(line, @"^\s*(\d+)\.\s+(.+)$");
            if (match.Success) result.Add((int.Parse(match.Groups[1].Value), MarkdownStructure.Plain(match.Groups[2].Value), ""));
        }
        return result;
    }
}
