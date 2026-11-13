using System.Text;
using System.Text.RegularExpressions;

namespace LinguaFlow.Core.Assessment;

public interface IExerciseGradingStrategy
{
    bool Supports(ExerciseKind kind);
    ExerciseFeedback Grade(ExerciseDefinition definition, ExerciseResponse response);
}

public sealed class ExerciseGradingService
{
    private readonly IReadOnlyList<IExerciseGradingStrategy> _strategies =
    [new ChoiceGradingStrategy(), new SetGradingStrategy(), new SequenceGradingStrategy(), new TextGradingStrategy(), new ManualReviewStrategy()];

    public ExerciseFeedback Grade(ExerciseDefinition definition, ExerciseResponse response)
    {
        if (response.ExerciseId != definition.ExerciseId) throw new ArgumentException("Exercise ID does not match.");
        if (response.Answers.Count == 0 || response.Answers.All(string.IsNullOrWhiteSpace)) throw new ArgumentException("An answer is required.");
        if (definition.MaximumScore <= 0) throw new ArgumentException("Maximum score must be positive.");
        var manual = definition.Kind is ExerciseKind.Writing or ExerciseKind.Speaking or ExerciseKind.Listening;
        var strategy = definition.AutomaticGrading && !manual ? _strategies.First(x => x.Supports(definition.Kind)) : _strategies.OfType<ManualReviewStrategy>().Single();
        return strategy.Grade(definition, response);
    }
}

internal static class AnswerComparison
{
    private static readonly Regex Space = new(@"\s+", RegexOptions.Compiled);
    public static string Normalize(string text, ExerciseDefinition definition)
    {
        var normalized = text.Normalize(NormalizationForm.FormC).Trim();
        normalized = Space.Replace(normalized, " ");
        if (definition.IgnorePunctuation) normalized = new string(normalized.Where(c => !char.IsPunctuation(c)).ToArray());
        return definition.CaseInsensitive ? normalized.ToUpperInvariant() : normalized;
    }
    public static ExerciseFeedback Result(ExerciseDefinition d, bool correct) => new()
    {
        Status = correct ? GradingStatus.Correct : GradingStatus.Incorrect,
        EarnedScore = correct ? d.MaximumScore : 0, MaximumScore = d.MaximumScore,
        Message = correct ? "FeedbackCorrect" : "FeedbackIncorrect",
        CorrectAnswer = string.Join(d.Kind == ExerciseKind.MultipleBlanks ? "; " : " / ", d.ValidAnswers), Explanation = d.Explanation
    };
}

public sealed class ChoiceGradingStrategy : IExerciseGradingStrategy
{
    public bool Supports(ExerciseKind kind) => kind is ExerciseKind.SingleChoice or ExerciseKind.TrueFalse;
    public ExerciseFeedback Grade(ExerciseDefinition d, ExerciseResponse r)
    {
        if (r.Answers.Count != 1 || d.ValidAnswers.Count == 0) throw new ArgumentException("One choice and an authored key are required.");
        var selected = r.Answers[0].Trim();
        if (d.Options.Count > 0 && !d.Options.Any(o => o.Equals(selected, StringComparison.OrdinalIgnoreCase) || o.StartsWith(selected + ".", StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("Selected choice is not in the question.");
        var letter = selected.Length > 1 && selected[1] == '.' ? selected[..1] : selected;
        return AnswerComparison.Result(d, d.ValidAnswers.Any(a => AnswerComparison.Normalize(a, d) == AnswerComparison.Normalize(letter, d)));
    }
}

public sealed class SetGradingStrategy : IExerciseGradingStrategy
{
    public bool Supports(ExerciseKind kind) => kind is ExerciseKind.MultipleSelect or ExerciseKind.Matching or ExerciseKind.MultipleBlanks;
    public ExerciseFeedback Grade(ExerciseDefinition d, ExerciseResponse r)
    {
        if (d.ValidAnswers.Count == 0) throw new ArgumentException("Authored answers are required.");
        var expected = d.ValidAnswers.Select(x => AnswerComparison.Normalize(x, d)).ToArray();
        var actual = r.Answers.Select(x => AnswerComparison.Normalize(x, d)).ToArray();
        if (d.Kind == ExerciseKind.MultipleSelect)
        {
            var choices = actual.Select(x => x.Length > 1 && x[1] == '.' ? x[..1] : x).ToArray();
            var correct = expected.ToHashSet().SetEquals(choices);
            return AnswerComparison.Result(d, correct);
        }
        var matches = expected.Zip(actual).Count(pair => pair.First == pair.Second);
        if (matches == expected.Length && actual.Length == expected.Length) return AnswerComparison.Result(d, true);
        if (d.AllowPartialCredit && matches > 0)
            return new ExerciseFeedback { Status = GradingStatus.Partial, EarnedScore = d.MaximumScore * matches / expected.Length, MaximumScore = d.MaximumScore, Message = "FeedbackPartial", CorrectAnswer = string.Join("; ", d.ValidAnswers), Explanation = d.Explanation };
        return AnswerComparison.Result(d, false);
    }
}

public sealed class SequenceGradingStrategy : IExerciseGradingStrategy
{
    public bool Supports(ExerciseKind kind) => kind is ExerciseKind.SentenceOrdering or ExerciseKind.WordOrdering;
    public ExerciseFeedback Grade(ExerciseDefinition d, ExerciseResponse r)
    {
        if (d.ValidAnswers.Count == 0) throw new ArgumentException("An authored sequence is required.");
        var correct = r.Answers.Count == d.ValidAnswers.Count && r.Answers.Zip(d.ValidAnswers).All(pair => AnswerComparison.Normalize(pair.First, d) == AnswerComparison.Normalize(pair.Second, d));
        return AnswerComparison.Result(d, correct);
    }
}

public sealed class TextGradingStrategy : IExerciseGradingStrategy
{
    public bool Supports(ExerciseKind kind) => kind is ExerciseKind.FillBlank or ExerciseKind.ShortAnswer or ExerciseKind.Reading or ExerciseKind.ErrorCorrection;
    public ExerciseFeedback Grade(ExerciseDefinition d, ExerciseResponse r)
    {
        if (r.Answers.Count != 1 || d.ValidAnswers.Count == 0) throw new ArgumentException("One answer and an authored key are required.");
        return AnswerComparison.Result(d, d.ValidAnswers.Any(x => AnswerComparison.Normalize(x, d) == AnswerComparison.Normalize(r.Answers[0], d)));
    }
}

public sealed class ManualReviewStrategy : IExerciseGradingStrategy
{
    public bool Supports(ExerciseKind kind) => true;
    public ExerciseFeedback Grade(ExerciseDefinition d, ExerciseResponse r) => new()
    {
        Status = d.Kind == ExerciseKind.Listening && !r.AudioEvidenceProvided ? GradingStatus.InsufficientEvidence : GradingStatus.NeedsReview,
        EarnedScore = 0, MaximumScore = d.MaximumScore,
        Message = d.Kind == ExerciseKind.Listening && !r.AudioEvidenceProvided ? "FeedbackNoAudio" : "FeedbackManual",
        CorrectAnswer = d.ValidAnswers.Count > 0 ? string.Join(" / ", d.ValidAnswers) : null,
        Explanation = d.Explanation
    };
}
