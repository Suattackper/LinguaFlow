using System.Text.RegularExpressions;
using LinguaFlow.Core.Courses;

namespace LinguaFlow.CourseConverter;

public sealed record ParsedUnit(UnitContent Content, IReadOnlyList<string> Warnings);

public static class UnitParser
{
    public static ParsedUnit Parse(int number, string courseId, string markdown, string? sourceFileOverride = null)
    {
        var warnings = new List<string>();
        var unitId = $"unit-{number:00}";
        var sourceFile = sourceFileOverride ?? $"LESSONS/Unit_{number:00}.md";
        var headings = MarkdownStructure.Split(markdown);
        if (headings.Count == 0) throw new InvalidOperationException($"{sourceFile} has no heading.");
        var titleMatch = Regex.Match(headings[0].Heading, @"^UNIT\s+(\d+)\s+[—–-]\s*(.+)$", RegexOptions.IgnoreCase);
        if (!titleMatch.Success || int.Parse(titleMatch.Groups[1].Value) != number) throw new InvalidOperationException($"{sourceFile} has a missing or mismatched Unit title.");
        var title = titleMatch.Groups[2].Value.Trim();
        var sections = new List<UnitSection>();
        var usedSections = new HashSet<string>(StringComparer.Ordinal);
        var usedExercises = new HashSet<string>(StringComparer.Ordinal);
        var objectives = new List<string>();
        var reviewDays = new List<int>();
        string? parent = null;
        foreach (var heading in headings)
        {
            var baseId = heading.Level == 1 ? "intro" : MarkdownStructure.StableSectionId(heading.Heading);
            var sectionId = baseId;
            for (var suffix = 2; !usedSections.Add(sectionId); suffix++) sectionId = $"{baseId}-{suffix}";
            if (sectionId != baseId) warnings.Add($"{sourceFile}: repeated heading ID {baseId}; assigned {sectionId}.");
            if (heading.Level == 2) parent = sectionId;
            var provenance = new SourceProvenance { File = sourceFile, OriginalUnit = $"{number:00}", OriginalSection = heading.Heading };
            var blocks = BuildBlocks(unitId, sectionId, heading, provenance, usedExercises, warnings);
            if (sectionId == "objectives") objectives.AddRange(heading.Body.Split('\n').Where(x => x.TrimStart().StartsWith("- ")).Select(x => MarkdownStructure.Plain(x.Trim()[2..])).Where(x => x.Length > 0));
            if (sectionId == "i")
            {
                foreach (var line in heading.Body.Split('\n').Where(x => x.Contains("Ôn", StringComparison.OrdinalIgnoreCase) && x.Contains("ngày", StringComparison.OrdinalIgnoreCase)))
                    reviewDays.AddRange(Regex.Matches(line, @"\b(?:1|3|7|14)\b").Select(x => int.Parse(x.Value)));
            }
            sections.Add(new UnitSection
            {
                SectionId = sectionId, Title = heading.Heading, Order = sections.Count + 1,
                ParentSectionId = heading.Level == 3 ? parent : null, OriginalHeading = heading.Heading,
                SourceMarkdown = heading.Raw, Source = provenance, Blocks = blocks
            });
        }
        if (objectives.Count == 0) warnings.Add($"{sourceFile}: no objectives parsed.");
        if (reviewDays.Count == 0) warnings.Add($"{sourceFile}: no spaced review days parsed.");
        var kind = number switch { 12 or 30 => "checkpoint", 36 => "capstone", 6 or 18 or 24 => "integrated-assessment", _ => "standard" };
        var unit = new UnitContent
        {
            SchemaVersion = 1, CourseId = courseId, UnitId = unitId, Title = title, Kind = kind,
            Objectives = objectives, ReviewScheduleDays = reviewDays.Distinct().OrderBy(x => x).ToList(),
            Source = new SourceProvenance { File = sourceFile, OriginalUnit = $"{number:00}", OriginalSection = headings[0].Heading },
            Sections = sections
        };
        return new ParsedUnit(unit, warnings);
    }

    private static List<ContentBlock> BuildBlocks(string unitId, string sectionId, MarkdownSection section, SourceProvenance source, HashSet<string> usedExercises, List<string> warnings)
    {
        var blocks = new List<ContentBlock>();
        var blockIndex = 0;
        string BlockId() => $"{unitId}-{sectionId}-block-{++blockIndex:00}";
        var body = section.Body.Trim();
        if (body.Length == 0) return blocks;
        var key = sectionId;
        if (key == "a")
        {
            blocks.Add(new VocabularyBlock { BlockId = BlockId(), Text = body, Source = source });
        }
        else if (key.StartsWith('b') && key is not "intro")
        {
            blocks.Add(new GrammarBlock { BlockId = BlockId(), Text = body, Source = source });
            foreach (var line in body.Split('\n'))
                if (Regex.IsMatch(line, @"\*\*(?:Example|Ví dụ):\*\*", RegexOptions.IgnoreCase))
                    blocks.Add(new ExampleBlock { BlockId = BlockId(), Text = MarkdownStructure.Plain(line.TrimStart('-', ' ')), Source = source });
        }
        else if (key is "c1" or "c2" or "c3" or "reading-questions" || key == "d")
        {
            var group = key == "reading-questions" ? "e" : key == "d" && body.Contains("**D1.", StringComparison.Ordinal) ? "d1" : key;
            var questions = MarkdownStructure.NumberedQuestions(body);
            if (questions.Count > 0)
            {
                var groupId = $"{unitId}-{group}-group";
                usedExercises.Add(groupId);
                var firstQuestion = Regex.Match(body, @"(?m)^\s*1\.\s+");
                var instructions = firstQuestion.Success ? body[..firstQuestion.Index].Trim() : "";
                blocks.Add(new ExerciseGroupBlock { BlockId = BlockId(), ExerciseId = groupId, Title = section.Heading, Instructions = instructions, Source = source });
                foreach (var question in questions)
                {
                    var exerciseId = $"{unitId}-{group}-{question.Number:00}";
                    if (!usedExercises.Add(exerciseId)) warnings.Add($"{source.File}: duplicate exercise {exerciseId}.");
                    var responseKind = group == "c2" && question.Options.Count > 0 ? "multiple-choice" : group == "c2" || group == "c3" ? "error-correction" : group == "e" ? "reading-response" : unitId == "unit-01" && group == "c1" && question.Number is 6 or 10 && Regex.Matches(question.Prompt, "___").Count == 2 ? "multiple-blanks" : "text";
                    blocks.Add(new QuestionBlock { BlockId = BlockId(), ExerciseId = exerciseId, Prompt = question.Prompt, Options = question.Options, ResponseKind = responseKind, Source = source });
                }
            }
            else blocks.Add(new ExplanationBlock { BlockId = BlockId(), Text = body, Source = source });
            if (key == "d")
            {
                var d2 = Regex.Match(body, @"(?m)^\*\*D2\..+$");
                if (d2.Success)
                {
                    var exerciseId = $"{unitId}-d2-writing";
                    usedExercises.Add(exerciseId);
                    blocks.Add(new WritingBlock { BlockId = BlockId(), ExerciseId = exerciseId, Prompt = MarkdownStructure.Plain(d2.Value), Source = source });
                }
            }
        }
        else if (key == "e") blocks.Add(new ReadingBlock { BlockId = BlockId(), Text = body, Source = source });
        else if (key == "f") blocks.Add(new WritingBlock { BlockId = BlockId(), ExerciseId = $"{unitId}-f-writing", Prompt = body, Source = source });
        else if (key is "g1" or "g2") blocks.Add(new SpeakingBlock { BlockId = BlockId(), ExerciseId = $"{unitId}-{key}-speaking", Prompt = body, Source = source });
        else if (key == "g3") blocks.Add(new ListeningBlock { BlockId = BlockId(), ExerciseId = $"{unitId}-g3-listening", Prompt = body, Source = source });
        else if (key == "i") blocks.Add(new HomeworkBlock { BlockId = BlockId(), Text = body, Source = source });
        else if (key == "h") blocks.Add(new ReviewBlock { BlockId = BlockId(), Text = body, Source = source });
        else blocks.Add(new ExplanationBlock { BlockId = BlockId(), Text = body, Source = source });
        return blocks;
    }
}
