using System.Text;
using LinguaFlow.Core.Courses;

namespace LinguaFlow.CourseConverter;

public static class ConversionReport
{
    public static string Create(CourseSources sources, int studentBookUnits, int answerBookUnits, int curriculumUnits,
        IReadOnlyList<UnitContent> units, VocabularyDocument vocabulary, AssessmentDocument assessments,
        IReadOnlyList<string> warnings, string fingerprint)
    {
        var blocks = units.SelectMany(x => x.Sections).SelectMany(x => x.Blocks).ToArray();
        var questions = blocks.OfType<QuestionBlock>().ToArray();
        var openTasks = blocks.Count(x => x is WritingBlock or SpeakingBlock or ListeningBlock);
        var autoCandidates = assessments.Answers.Count(x => x.AssessmentMode.StartsWith("auto-", StringComparison.Ordinal));
        var missing = warnings.Count(x => x.Contains("missing answer", StringComparison.OrdinalIgnoreCase));
        var ambiguous = warnings.Count(x => x.Contains("manual review required", StringComparison.OrdinalIgnoreCase));
        var unresolved = warnings.Count(x => x.Contains("differs", StringComparison.OrdinalIgnoreCase) || x.Contains("absent", StringComparison.OrdinalIgnoreCase) || x.Contains("missing", StringComparison.OrdinalIgnoreCase) || x.Contains("no lesson exercise", StringComparison.OrdinalIgnoreCase));
        var text = new StringBuilder();
        text.AppendLine("# English course conversion report").AppendLine();
        text.AppendLine("Generated from the discovered original source files by `LinguaFlow.CourseConverter`. Counts describe the generated LCF content; a rubric or answer key is not an implemented grader.").AppendLine();
        text.AppendLine("## Source inventory and coverage").AppendLine();
        text.AppendLine($"- Course directory: `CourseSources/English/{Path.GetFileName(sources.Directory)}`");
        text.AppendLine($"- Original files discovered: **{sources.AllFiles.Count}** ({sources.AllFiles.Count(x => Path.GetExtension(x).Equals(".md", StringComparison.OrdinalIgnoreCase))} Markdown, {sources.AllFiles.Count(x => Path.GetExtension(x).Equals(".pdf", StringComparison.OrdinalIgnoreCase))} PDF)");
        text.AppendLine($"- Individual lessons: **{sources.Lessons.Count}**; combined Student Book Unit boundaries: **{studentBookUnits}**; Curriculum table Units: **{curriculumUnits}**");
        text.AppendLine($"- Individual answer keys: **{sources.AnswerKeys.Count}**; combined Answer Key Book Unit boundaries: **{answerBookUnits}**");
        text.AppendLine("- Source priority: individual lessons and answer keys; combined books and Curriculum used for cross-checks; vocabulary bank for global indexing; GPT-TEACHER for practice configuration.");
        text.AppendLine("- PDFs, README, and `TRACKING/PROGRESS.md` were discovered but not published as content. Source files were not modified.").AppendLine();
        text.AppendLine("## Conversion totals").AppendLine();
        text.AppendLine($"- Units detected/converted: **{units.Count}/{units.Count}**");
        text.AppendLine($"- Sections converted with source Markdown and provenance: **{units.Sum(x => x.Sections.Count)}**");
        text.AppendLine($"- Numbered exercises: **{questions.Length}**; open writing/speaking/listening tasks: **{openTasks}**");
        text.AppendLine($"- Grammar blocks: **{blocks.OfType<GrammarBlock>().Count()}**; extracted examples: **{blocks.OfType<ExampleBlock>().Count()}**; reading blocks: **{blocks.OfType<ReadingBlock>().Count()}**; vocabulary blocks: **{blocks.OfType<VocabularyBlock>().Count()}**");
        text.AppendLine($"- Writing tasks: **{blocks.OfType<WritingBlock>().Count()}**; speaking tasks: **{blocks.OfType<SpeakingBlock>().Count()}**; listening tasks: **{blocks.OfType<ListeningBlock>().Count()}**; homework blocks: **{blocks.OfType<HomeworkBlock>().Count()}**");
        text.AppendLine($"- Units with parsed review schedules: **{units.Count(x => x.ReviewScheduleDays.Count > 0)}**");
        text.AppendLine($"- Automatically gradable **candidates** identified from unambiguous multiple-choice keys: **{autoCandidates}**. Automatic grading is not implemented in Step 03.");
        text.AppendLine($"- Manual-review numbered exercises: **{questions.Length - autoCandidates}**; additional open tasks: **{openTasks}**");
        text.AppendLine($"- Matched answer-key items: **{assessments.Answers.Count}**; missing numbered answers: **{missing}**; ambiguous/multi-part mappings flagged: **{ambiguous}**");
        text.AppendLine($"- Vocabulary bank entries: **{vocabulary.Entries.Count}** across **{vocabulary.Entries.Select(x => x.UnitId).Distinct().Count()}** Units");
        text.AppendLine($"- Rubric sections: **{assessments.Rubrics.Count}**; threshold/rule lines: **{assessments.Thresholds.Count}**");
        text.AppendLine($"- Authored dialogue passages detected: **{blocks.OfType<DialogueBlock>().Count()}**. Role-play and dialogue-writing instructions remain speaking/writing tasks; no dialogue was invented.");
        text.AppendLine($"- Unresolved source discrepancies/missing references: **{unresolved}**; see warnings below.");
        text.AppendLine($"- Deterministic content fingerprint: `{fingerprint}`").AppendLine();
        text.AppendLine("## Representative source review").AppendLine();
        foreach (var number in new[] { 1, 18, 36 })
        {
            var unit = units.FirstOrDefault(x => x.UnitId == $"unit-{number:00}");
            if (unit is null) { text.AppendLine($"- Unit {number:00}: missing."); continue; }
            var count = unit.Sections.SelectMany(x => x.Blocks).OfType<QuestionBlock>().Count();
            text.AppendLine($"- Unit {number:00} — **{unit.Title}**: {unit.Objectives.Count} objectives, {unit.Sections.Count} sections, {count} numbered exercises, {assessments.Answers.Count(x => x.UnitId == unit.UnitId)} matched answer items; source `LESSONS/Unit_{number:00}.md` and `ANSWER_KEYS/Unit_{number:00}_Answers.md`.");
        }
        text.AppendLine().AppendLine("## Conversion warnings and manual review").AppendLine();
        if (warnings.Count == 0) text.AppendLine("No source discrepancies or unresolved mappings detected by the converter. This does not replace educational review.");
        else foreach (var warning in warnings.OrderBy(x => x, StringComparer.Ordinal)) text.AppendLine($"- {warning}");
        text.AppendLine().AppendLine("## Files discovered").AppendLine();
        foreach (var file in sources.AllFiles.OrderBy(x => x, StringComparer.Ordinal))
            text.AppendLine($"- `{Path.GetRelativePath(sources.Directory, file).Replace('\\', '/')}`");
        return text.ToString();
    }
}
