using System.Text.Json.Serialization;

namespace LinguaFlow.Core.Courses;

public enum CourseStatus { Available, ComingSoon, Archived }

public sealed record CourseCatalog
{
    public required int SchemaVersion { get; init; }
    public required List<CourseCatalogEntry> Courses { get; init; }
}

public sealed record CourseCatalogEntry
{
    public required string CourseId { get; init; }
    public required string LanguageCode { get; init; }
    public required string LanguageDirectory { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Level { get; init; }
    public required CourseStatus Status { get; init; }
    public required string ManifestPath { get; init; }
    public string? CoverImage { get; init; }
    public List<string>? Tags { get; init; }
}

public sealed record CourseManifest
{
    public required int SchemaVersion { get; init; }
    public required string CourseId { get; init; }
    public required string ContentVersion { get; init; }
    public required string ContentFingerprint { get; init; }
    public required string LanguageCode { get; init; }
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required string Description { get; init; }
    public required string LearningLevel { get; init; }
    public required List<UnitReference> Units { get; init; }
    public required List<ContentReference> Vocabulary { get; init; }
    public required List<ContentReference> Assessments { get; init; }
    public required List<ContentReference> AiPractice { get; init; }
    public required List<DownloadReference> Downloads { get; init; }
    public List<ContentReference>? MediaAssets { get; init; }
    public required CompletionRules CompletionRules { get; init; }
}

public sealed record UnitReference
{
    public required string UnitId { get; init; }
    public required string Title { get; init; }
    public required int Order { get; init; }
    public required string Path { get; init; }
}

public sealed record ContentReference
{
    public required string Id { get; init; }
    public required string Path { get; init; }
}

public sealed record DownloadReference
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Path { get; init; }
    public string Description { get; init; } = "";
    public string Category { get; init; } = "supplementary";
    public string Format { get; init; } = "";
    public long? FileSize { get; init; }
    public string SourceLanguage { get; init; } = "";
    public int DisplayOrder { get; init; }
    public bool Available { get; init; } = true;
}

public sealed record CompletionRules
{
    public required string Kind { get; init; }
    public int? MinimumCompletedUnits { get; init; }
}

public sealed record UnitContent
{
    public required int SchemaVersion { get; init; }
    public required string CourseId { get; init; }
    public required string UnitId { get; init; }
    public required string Title { get; init; }
    public required List<UnitSection> Sections { get; init; }
    public string Kind { get; init; } = "standard";
    public List<string> Objectives { get; init; } = [];
    public List<int> ReviewScheduleDays { get; init; } = [];
    public SourceProvenance? Source { get; init; }
}

public sealed record UnitSection
{
    public required string SectionId { get; init; }
    public required string Title { get; init; }
    public required List<ContentBlock> Blocks { get; init; }
    public int Order { get; init; }
    public string? ParentSectionId { get; init; }
    public string? OriginalHeading { get; init; }
    public string? SourceMarkdown { get; init; }
    public SourceProvenance? Source { get; init; }
}

public sealed record SourceProvenance
{
    public required string File { get; init; }
    public required string OriginalUnit { get; init; }
    public required string OriginalSection { get; init; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(HeadingBlock), "heading")]
[JsonDerivedType(typeof(ExplanationBlock), "explanation")]
[JsonDerivedType(typeof(GrammarBlock), "grammar")]
[JsonDerivedType(typeof(ExampleBlock), "example")]
[JsonDerivedType(typeof(DialogueBlock), "dialogue")]
[JsonDerivedType(typeof(ReadingBlock), "reading")]
[JsonDerivedType(typeof(VocabularyBlock), "vocabulary")]
[JsonDerivedType(typeof(QuestionBlock), "question")]
[JsonDerivedType(typeof(ExerciseGroupBlock), "exercise-group")]
[JsonDerivedType(typeof(WritingBlock), "writing")]
[JsonDerivedType(typeof(SpeakingBlock), "speaking")]
[JsonDerivedType(typeof(ListeningBlock), "listening")]
[JsonDerivedType(typeof(HomeworkBlock), "homework")]
[JsonDerivedType(typeof(ReviewBlock), "review")]
[JsonDerivedType(typeof(ImageBlock), "image")]
[JsonDerivedType(typeof(AudioBlock), "audio")]
public abstract record ContentBlock
{
    public required string BlockId { get; init; }
    public bool Optional { get; init; }
    public Dictionary<string, string>? LocalizedText { get; init; }
    public SourceProvenance? Source { get; init; }
}

public sealed record HeadingBlock : ContentBlock { public required string Text { get; init; } }
public sealed record ExplanationBlock : ContentBlock { public required string Text { get; init; } }
public sealed record GrammarBlock : ContentBlock { public required string Text { get; init; } }
public sealed record ExampleBlock : ContentBlock { public required string Text { get; init; } }
public sealed record DialogueBlock : ContentBlock { public required string Text { get; init; } }
public sealed record ReadingBlock : ContentBlock { public required string Text { get; init; } }
public sealed record VocabularyBlock : ContentBlock { public required string Text { get; init; } public List<VocabularyEntry>? Entries { get; init; } }
public sealed record QuestionBlock : ContentBlock { public required string ExerciseId { get; init; } public required string Prompt { get; init; } public List<string>? Options { get; init; } public string? ResponseKind { get; init; } }
public sealed record ExerciseGroupBlock : ContentBlock { public required string ExerciseId { get; init; } public required string Title { get; init; } public string? Instructions { get; init; } }
public sealed record WritingBlock : ContentBlock { public required string ExerciseId { get; init; } public required string Prompt { get; init; } }
public sealed record SpeakingBlock : ContentBlock { public required string ExerciseId { get; init; } public required string Prompt { get; init; } }
public sealed record ListeningBlock : ContentBlock { public required string ExerciseId { get; init; } public required string Prompt { get; init; } }
public sealed record HomeworkBlock : ContentBlock { public required string Text { get; init; } }
public sealed record ReviewBlock : ContentBlock { public required string Text { get; init; } }
public sealed record ImageBlock : ContentBlock { public required string Path { get; init; } public required string Alt { get; init; } }
public sealed record AudioBlock : ContentBlock { public required string Path { get; init; } public required string Transcript { get; init; } }

public sealed record VocabularyDocument
{
    public required int SchemaVersion { get; init; }
    public required string CourseId { get; init; }
    public required List<VocabularyEntry> Entries { get; init; }
}

public sealed record VocabularyEntry
{
    public required string VocabularyId { get; init; }
    public required string UnitId { get; init; }
    public required string Term { get; init; }
    public required string Ipa { get; init; }
    public required string Vietnamese { get; init; }
    public required string Context { get; init; }
    public required SourceProvenance Source { get; init; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] public string? PartOfSpeech { get; init; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] public string? Example { get; init; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] public string? Explanation { get; init; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] public string? Topic { get; init; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] public string? AudioPath { get; init; }
}

public sealed record AssessmentDocument
{
    public required int SchemaVersion { get; init; }
    public required string CourseId { get; init; }
    public required List<AnswerRecord> Answers { get; init; }
    public required List<RubricRecord> Rubrics { get; init; }
    public required List<ThresholdRule> Thresholds { get; init; }
}

public sealed record AnswerRecord
{
    public required string ExerciseId { get; init; }
    public required string UnitId { get; init; }
    public required string Answer { get; init; }
    public required string Explanation { get; init; }
    public required string AssessmentMode { get; init; }
    public required SourceProvenance Source { get; init; }
    public bool Ambiguous { get; init; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? ValidAnswers { get; init; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public bool AllowPartialCredit { get; init; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public bool IgnorePunctuation { get; init; }
}

public sealed record RubricRecord
{
    public required string RubricId { get; init; }
    public required string UnitId { get; init; }
    public required string Skill { get; init; }
    public required string Text { get; init; }
    public required SourceProvenance Source { get; init; }
}

public sealed record ThresholdRule
{
    public required string UnitId { get; init; }
    public required string Skill { get; init; }
    public required string Text { get; init; }
    public int? MinimumScore { get; init; }
    public int? TotalScore { get; init; }
    public int? MinimumPercent { get; init; }
}

public sealed record PracticeConfiguration
{
    public required int SchemaVersion { get; init; }
    public required string CourseId { get; init; }
    public required string Instructions { get; init; }
    public required List<string> Modes { get; init; }
    public required bool AudioEvidenceRequiredForListening { get; init; }
    public required bool AudioEvidenceRequiredForPronunciation { get; init; }
    public required SourceProvenance Source { get; init; }
}
