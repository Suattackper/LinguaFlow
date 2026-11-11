using LinguaFlow.Core.Assessment;

namespace LinguaFlow.Web.Services;

public enum ExerciseUiState
{
    NotStarted, Editing, Submitted, Correct, Incorrect, AcceptedVariant,
    SelfReviewNeeded, PendingExternalEvaluation, Evaluated, Locked, Unavailable,
    InsufficientEvidence, Partial
}

/// <summary>Presentation only. The stored grading result is the sole source of scoring truth.</summary>
public static class ExerciseStatePresenter
{
    public static ExerciseUiState Resolve(ExerciseDefinition definition, string draft, ExerciseAttempt? attempt, ExerciseMode mode, bool testReleased)
    {
        if (mode == ExerciseMode.Test && !testReleased) return ExerciseUiState.Locked;
        if (attempt is null) return string.IsNullOrWhiteSpace(draft) ? ExerciseUiState.NotStarted : ExerciseUiState.Editing;
        return attempt.Feedback.Status switch
        {
            GradingStatus.Correct when IsAuthoredAlternative(definition, attempt) => ExerciseUiState.AcceptedVariant,
            GradingStatus.Correct => ExerciseUiState.Correct,
            GradingStatus.Incorrect => ExerciseUiState.Incorrect,
            GradingStatus.Partial => ExerciseUiState.Partial,
            GradingStatus.InsufficientEvidence => ExerciseUiState.InsufficientEvidence,
            GradingStatus.NeedsReview when definition.Kind is ExerciseKind.Writing or ExerciseKind.Speaking or ExerciseKind.Listening => ExerciseUiState.PendingExternalEvaluation,
            GradingStatus.NeedsReview => ExerciseUiState.SelfReviewNeeded,
            _ => ExerciseUiState.Submitted
        };
    }

    private static bool IsAuthoredAlternative(ExerciseDefinition definition, ExerciseAttempt attempt)
    {
        if (definition.ValidAnswers.Count < 2 || attempt.Answers.Count != 1) return false;
        var answer = attempt.Answers[0].Trim();
        return !answer.Equals(definition.ValidAnswers[0].Trim(), StringComparison.OrdinalIgnoreCase)
            && definition.ValidAnswers.Skip(1).Any(x => answer.Equals(x.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
