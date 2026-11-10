using LinguaFlow.Core.Courses;

namespace LinguaFlow.Core.Assessment;

public static class ExerciseDefinitionFactory
{
    public static ExerciseDefinition Create(string courseId, string unitId, UnitSection section, ContentBlock block, AnswerRecord? key)
    {
        var (id, prompt, kind, options) = block switch
        {
            QuestionBlock q => (q.ExerciseId, q.Prompt, QuestionKind(q, section), (IReadOnlyList<string>)(q.Options ?? [])),
            WritingBlock w => (w.ExerciseId, w.Prompt, ExerciseKind.Writing, (IReadOnlyList<string>)[]),
            SpeakingBlock s => (s.ExerciseId, s.Prompt, ExerciseKind.Speaking, (IReadOnlyList<string>)[]),
            ListeningBlock l => (l.ExerciseId, l.Prompt, ExerciseKind.Listening, (IReadOnlyList<string>)[]),
            _ => throw new ArgumentException("Block has no learner response.")
        };
        var automatic = key is { Ambiguous: false } && ((kind is ExerciseKind.SingleChoice or ExerciseKind.TrueFalse && key.AssessmentMode == "auto-choice") || (key.AssessmentMode == "auto-exact" && kind is not (ExerciseKind.Writing or ExerciseKind.Speaking or ExerciseKind.Listening)));
        var answers = key?.ValidAnswers is { Count: > 0 } ? key.ValidAnswers : key is not null ? [key.Answer] : [];
        return new ExerciseDefinition
        {
            CourseId = courseId, UnitId = unitId, ExerciseId = id, Prompt = prompt, Kind = kind,
            Skill = Skill(section.SectionId, kind), Options = options, ValidAnswers = answers,
            Explanation = key?.Explanation, AutomaticGrading = automatic,
            AllowPartialCredit = key?.AllowPartialCredit == true, IgnorePunctuation = key?.IgnorePunctuation == true
        };
    }

    private static ExerciseKind QuestionKind(QuestionBlock q, UnitSection section) => q.ResponseKind switch
    {
        "multiple-choice" => ExerciseKind.SingleChoice,
        "multiple-select" => ExerciseKind.MultipleSelect,
        "true-false" => ExerciseKind.TrueFalse,
        "matching" => ExerciseKind.Matching,
        "sentence-ordering" => ExerciseKind.SentenceOrdering,
        "word-ordering" => ExerciseKind.WordOrdering,
        "multiple-blanks" => ExerciseKind.MultipleBlanks,
        "fill-blank" => ExerciseKind.FillBlank,
        "error-correction" => ExerciseKind.ErrorCorrection,
        "reading-response" => ExerciseKind.Reading,
        "short-answer" => ExerciseKind.ShortAnswer,
        _ when section.SectionId.StartsWith("c1", StringComparison.OrdinalIgnoreCase) || section.SectionId.StartsWith("d", StringComparison.OrdinalIgnoreCase) => ExerciseKind.FillBlank,
        _ => ExerciseKind.ShortAnswer
    };
    private static string Skill(string sectionId, ExerciseKind kind) => kind switch
    {
        ExerciseKind.Writing => "writing", ExerciseKind.Speaking => "speaking", ExerciseKind.Listening => "listening",
        _ when sectionId.StartsWith('c') => "grammar", _ when sectionId.StartsWith('d') => "vocabulary", _ when sectionId.StartsWith('e') || sectionId == "reading-questions" => "reading", _ => "practice"
    };
}
