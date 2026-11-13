using System.Text.Json;
using LinguaFlow.Core.Courses;
using LinguaFlow.Core.Progress;
using LinguaFlow.Core.Vocabulary;
using Xunit;

namespace LinguaFlow.Core.Tests;

public sealed class BackupValidatorTests
{
    private readonly BackupValidator _validator = new();
    private static CourseManifest Manifest => CourseFormatTests.Manifest();
    private static IReadOnlyDictionary<string, UnitContent> Units => new Dictionary<string, UnitContent> { ["unit-01"] = CourseFormatTests.Unit() };
    private static LearningRecord Record(string id = "position") => new()
    {
        CourseId = "sample", Kind = LearningRecordKind.SectionProgress, Id = id,
        UnitId = "unit-01", SectionId = "section-01", Data = JsonSerializer.SerializeToElement("section-01"), UpdatedAt = DateTimeOffset.UtcNow, RecordSchemaVersion = 1
    };
    private static CourseProgressBackup Backup() => new()
    {
        Format = "linguaflow-course-progress", BackupSchemaVersion = 1,
        CourseId = "sample", ContentVersion = Manifest.ContentVersion, ContentFingerprint = Manifest.ContentFingerprint,
        ExportedAt = DateTimeOffset.UtcNow, Progress = [Record()], Attempts = [], ReviewStates = [], Notes = [],
        Bookmarks = [], StudySessions = [], AiPracticeResults = [], CourseSettings = [], Drafts = [], VocabularyStates = [], LearningGoals = []
    };

    [Fact] public void ValidBackup_HasNoErrors() => Assert.Empty(_validator.Validate(Backup(), Manifest, Units));
    [Fact] public void WrongCourse_IsRejected() => Assert.Contains(_validator.Validate(Backup() with { CourseId = "other" }, Manifest, Units), e => e.Contains("different course"));
    [Fact] public void ChangedContent_RequiresMigration() => Assert.Contains(_validator.Validate(Backup() with { ContentFingerprint = "sha256:other" }, Manifest, Units), e => e.Contains("migration"));
    [Fact] public void Unit01GradingPolicyMigrationPreservesEarlierProgressBackup()
    {
        var current = Manifest with { CourseId = "english-life-office-it", ContentVersion = "1.1.0", ContentFingerprint = "sha256:a5bf7267189c280419cdfdc4459e1e36d6eeb87009292faf923310ba6211d517" };
        var earlier = Backup() with { CourseId = current.CourseId, ContentVersion = current.ContentVersion, ContentFingerprint = "sha256:0818b5eb0a159e10d19257a1b1af01849ee02055215fbda78a9757522807261c", Progress = [] };
        Assert.Empty(_validator.Validate(earlier, current, Units));
        Assert.False(CourseContentCompatibility.Accepts(current, "1.1.0", "sha256:other"));
    }
    [Fact] public void UnsupportedSchema_IsRejected() => Assert.Contains(_validator.Validate(Backup() with { BackupSchemaVersion = 2 }, Manifest, Units), e => e.Contains("schema"));
    [Fact] public void CorruptedAndDuplicateIds_AreRejected()
    {
        var duplicate = Backup() with { Progress = [Record(), Record()] };
        Assert.Contains(_validator.Validate(duplicate, Manifest, Units), e => e.Contains("Duplicate"));
        var bad = Backup() with { Progress = [Record("../bad") with { SectionId = "missing" }] };
        var errors = _validator.Validate(bad, Manifest, Units);
        Assert.Contains(errors, e => e.Contains("Invalid record ID"));
        Assert.Contains(errors, e => e.Contains("Unknown section"));
    }
    [Fact] public void RepeatedBackupJson_IsDeterministicForSameTimestamp()
    {
        var backup = Backup();
        var json = JsonSerializer.Serialize(backup);
        var copy = JsonSerializer.Deserialize<CourseProgressBackup>(json);
        Assert.NotNull(copy);
        Assert.Equal(json, JsonSerializer.Serialize(copy));
    }
    [Fact]
    public void CorruptedVocabularyReviewState_IsRejectedBeforeImport()
    {
        var word = new VocabularyEntry { VocabularyId = "unit-01-vocab-01", UnitId = "unit-01", Term = "task", Ipa = "", Vietnamese = "nhiệm vụ", Context = "", Source = new SourceProvenance { File = "VOCABULARY_BANK.md", OriginalUnit = "01", OriginalSection = "Unit 01" } };
        var unit = CourseFormatTests.Unit() with { Sections = [new UnitSection { SectionId = "vocab", Title = "Vocabulary", Blocks = [new VocabularyBlock { BlockId = "vocab-block", Text = "", Entries = [word] }] }] };
        var badState = new VocabularyReviewState { CourseId = "sample", UnitId = "unit-01", VocabularyId = word.VocabularyId, ReviewCount = 1, SuccessCount = 0, FailureCount = 0 };
        var record = new LearningRecord { CourseId = "sample", UnitId = "unit-01", VocabularyId = word.VocabularyId, Kind = LearningRecordKind.VocabularyState, Id = word.VocabularyId, Data = JsonSerializer.SerializeToElement(badState), UpdatedAt = DateTimeOffset.UtcNow, RecordSchemaVersion = 1 };
        var backup = Backup() with { VocabularyStates = [record] };
        Assert.Contains(_validator.Validate(backup, Manifest, new Dictionary<string, UnitContent> { ["unit-01"] = unit }), e => e.Contains("Invalid vocabulary review state"));
    }
}
