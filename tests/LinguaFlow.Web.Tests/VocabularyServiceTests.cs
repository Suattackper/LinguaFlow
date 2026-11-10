using System.Text.Json;
using LinguaFlow.Core.Courses;
using LinguaFlow.Core.Progress;
using LinguaFlow.Core.Vocabulary;
using LinguaFlow.Web.Services;
using Xunit;

namespace LinguaFlow.Web.Tests;

public sealed class VocabularyServiceTests
{
    private static VocabularyEntry Word => new() { VocabularyId = "unit-01-vocab-01", UnitId = "unit-01", Term = "task", Ipa = "", Vietnamese = "nhiệm vụ", Context = "", Source = new SourceProvenance { File = "VOCABULARY_BANK.md", OriginalUnit = "01", OriginalSection = "Unit 01" } };
    [Fact]
    public async Task ReviewRejectsCrossCourseStateBeforeStorageWrite()
    {
        var service = new VocabularyLearningService(null!, new SpacedRepetitionService());
        var wrong = VocabularyLearningService.NewState("other", Word);
        await Assert.ThrowsAsync<ArgumentException>(() => service.ReviewAsync("sample", Word, wrong, true, [1, 3, 7, 14], DateTimeOffset.UtcNow));
    }
    [Fact]
    public void BackupRoundTripIncludesVocabularyStateAndHistory()
    {
        var now = DateTimeOffset.UtcNow;
        var state = new SpacedRepetitionService().Review(VocabularyLearningService.NewState("sample", Word), true, now);
        var record = new LearningRecord { CourseId = "sample", UnitId = Word.UnitId, VocabularyId = Word.VocabularyId, Kind = LearningRecordKind.VocabularyState, Id = Word.VocabularyId, Data = JsonSerializer.SerializeToElement(state), UpdatedAt = now, RecordSchemaVersion = 1 };
        var backup = CourseBackupService.CreateBackup(CourseFormatFixture.Manifest(), [record], now);
        var copy = JsonSerializer.Deserialize<CourseProgressBackup>(JsonSerializer.Serialize(backup));
        Assert.NotNull(copy);
        var restored = copy.VocabularyStates.Single().Data.Deserialize<VocabularyReviewState>();
        Assert.NotNull(restored);
        Assert.Equal(state.NextDueAt, restored.NextDueAt);
        Assert.Single(restored.History);
    }
    [Fact]
    public void VoiceSelectionRespectsLanguageAndPreference()
    {
        SpeechVoice[] voices = [new("gb", "British", "en-GB", true), new("us", "American", "en-US", true), new("ja", "Japanese", "ja-JP", true)];
        Assert.Equal("us", TextToSpeechService.ChooseVoice(voices, "en-US", "us")?.VoiceUri);
        Assert.Equal("ja", TextToSpeechService.ChooseVoice(voices, "ja-JP", null)?.VoiceUri);
        Assert.Null(TextToSpeechService.ChooseVoice(voices, "zh-CN", null));
    }
}
