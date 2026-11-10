using LinguaFlow.Core.Courses;
using LinguaFlow.Core.Vocabulary;
using Xunit;

namespace LinguaFlow.Core.Tests;

public sealed class SpacedRepetitionTests
{
    private readonly SpacedRepetitionService _service = new();
    private static VocabularyReviewState New(string course = "sample") => new() { CourseId = course, UnitId = "unit-01", VocabularyId = "unit-01-vocab-01" };
    [Fact]
    public void ReviewsFollowOriginalIntervalsAndStopAtLastStage()
    {
        var now = new DateTimeOffset(2026, 11, 9, 8, 0, 0, TimeSpan.Zero);
        var state = New();
        foreach (var (day, expected) in new[] { (1, 1), (3, 2), (7, 3), (14, 4), (14, 4) })
        {
            state = _service.Review(state, true, now, [1, 3, 7, 14]);
            Assert.Equal(expected, state.ReviewStage);
            Assert.Equal(now.AddDays(day), state.NextDueAt);
        }
        Assert.Equal(5, state.ReviewCount);
        Assert.Equal(VocabularyLearningStatus.Known, state.Status);
        Assert.Equal(5, state.History.Count);
    }
    [Fact]
    public void UnitSpecificIntervalsOverrideDefaultAndFailureReturnsEarlierStage()
    {
        var now = DateTimeOffset.UtcNow;
        var state = _service.Review(New(), true, now, [2, 5]);
        Assert.Equal(now.AddDays(2), state.NextDueAt);
        state = _service.Review(state, false, now.AddHours(1), [2, 5]);
        Assert.Equal(0, state.ReviewStage);
        Assert.Equal(now.AddHours(1), state.NextDueAt);
        Assert.Equal(VocabularyLearningStatus.Difficult, state.Status);
        Assert.True(SpacedRepetitionService.IsDue(state, now.AddHours(1)));
        Assert.Equal(1, state.FailureCount);
    }
    [Fact]
    public void DueClassificationUsesAbsoluteTimeAndLocalCalendar()
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("UTC+7", TimeSpan.FromHours(7), "UTC+7", "UTC+7");
        var now = new DateTimeOffset(2026, 11, 9, 0, 30, 0, TimeSpan.Zero);
        var state = New() with { NextDueAt = new DateTimeOffset(2026, 11, 8, 23, 30, 0, TimeSpan.Zero) };
        Assert.True(SpacedRepetitionService.IsDue(state, now));
        Assert.True(SpacedRepetitionService.IsDueToday(state, now, zone));
        Assert.False(SpacedRepetitionService.IsOverdue(state, now, zone));
        Assert.True(SpacedRepetitionService.IsOverdue(state, now, TimeZoneInfo.Utc));
    }
    [Fact]
    public void InvalidScheduleIsRejectedWithoutChangingState()
    {
        Assert.Throws<ArgumentException>(() => _service.Review(New(), true, DateTimeOffset.UtcNow, [1, 1, 7]));
    }
    [Fact]
    public void DuplicateVocabularyIdsAndUnsafeAudioAreRejected()
    {
        var source = new SourceProvenance { File = "VOCABULARY_BANK.md", OriginalUnit = "01", OriginalSection = "Unit 01" };
        var word = new VocabularyEntry { VocabularyId = "unit-01-vocab-01", UnitId = "unit-01", Term = "task", Ipa = "", Vietnamese = "nhiệm vụ", Context = "", Source = source };
        var document = new VocabularyDocument { SchemaVersion = 1, CourseId = "sample", Entries = [word, word] };
        Assert.Contains(new CourseContentValidator().ValidateVocabulary(document, CourseFormatTests.Manifest()), x => x.Contains("duplicate"));
        Assert.Contains(new CourseContentValidator().ValidateVocabulary(document with { Entries = [word with { AudioPath = "../secret.mp3" }] }, CourseFormatTests.Manifest()), x => x.Contains("audio path"));
    }
}
