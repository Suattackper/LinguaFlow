using System.Text.Json;
using LinguaFlow.Core.Courses;
using LinguaFlow.Core.Practice;
using Xunit;

namespace LinguaFlow.Core.Tests;

public sealed class PracticePackageTests
{
    private static string CourseRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/LinguaFlow.Web/wwwroot/Courses/English/english-life-office-it"));
    private static T Read<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(CourseRoot, path)), CourseJson.Options)!;
    private static (CourseManifest Manifest, UnitContent Unit, AssessmentDocument Assessments, VocabularyDocument Vocabulary, PracticeConfiguration Config) Source()
        => (Read<CourseManifest>("manifest.json"), Read<UnitContent>("Units/unit-01.json"), Read<AssessmentDocument>("Assessments/assessments.json"), Read<VocabularyDocument>("Vocabulary/vocabulary.json"), Read<PracticeConfiguration>("Prompts/practice-config.json"));

    [Fact]
    public void RealSpeakingTaskExportsSelfContainedVersionedPackage()
    {
        var (manifest, unit, assessments, vocabulary, config) = Source();
        var task = unit.Sections.SelectMany(s => s.Blocks).OfType<SpeakingBlock>().First();
        var rubric = assessments.Rubrics.Single(r => r.UnitId == unit.UnitId && r.Skill.Contains("speaking"));
        var request = PracticePackageBuilder.Create(manifest, unit, task, rubric, config, vocabulary, "vi-VN", "standard", 15);
        var markdown = PracticePackageBuilder.BuildMarkdown(request);
        Assert.Equal("linguaflow-practice-request", request.Format);
        Assert.Equal(manifest.ContentFingerprint, request.CourseContentFingerprint);
        Assert.Equal(task.ExerciseId, request.TaskId);
        Assert.Equal(100, request.AssessmentCriteria.Sum(c => c.MaxPoints));
        var envelope = markdown.Split("````json\n", StringSplitOptions.None)[1].Split("\n````", StringSplitOptions.None)[0];
        Assert.Equal(task.Prompt, JsonSerializer.Deserialize<PracticeRequest>(envelope, new JsonSerializerOptions(JsonSerializerDefaults.Web))!.PracticeInstructions);
        Assert.Contains("linguaflow-practice-result", markdown);
        Assert.Contains("NOT ASSESSED", markdown);
        Assert.Contains("one question at a time", markdown);
        Assert.Contains("external-ai:ChatGPT", markdown);
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(markdown) < PracticePackageBuilder.MaximumBytes);
        Assert.Matches(@"^linguaflow-speaking-unit-01-[0-9a-f]{32}\.md$", PracticePackageBuilder.Filename(request));
        Assert.NotEmpty(request.RelevantVocabulary);
        Assert.NotEmpty(request.RelevantGrammar);
    }

    [Fact]
    public void RequestsAreUniqueAndForeignReferencesAreRejected()
    {
        var (manifest, unit, assessments, vocabulary, config) = Source();
        var task = unit.Sections.SelectMany(s => s.Blocks).OfType<SpeakingBlock>().First();
        var rubric = assessments.Rubrics.Single(r => r.UnitId == unit.UnitId && r.Skill.Contains("speaking"));
        var a = PracticePackageBuilder.Create(manifest, unit, task, rubric, config, vocabulary, "en-US", "guided", 10);
        var b = PracticePackageBuilder.Create(manifest, unit, task, rubric, config, vocabulary, "en-US", "guided", 10);
        Assert.NotEqual(a.RequestId, b.RequestId);
        Assert.Equal("en-US", a.LearnerDisplayLanguage);
        Assert.Equal(manifest.LanguageCode, a.TargetLanguage);
        Assert.Throws<ArgumentException>(() => PracticePackageBuilder.Create(manifest, unit with { CourseId = "other" }, task, rubric, config, vocabulary, "en-US", "guided", 10));
        Assert.Throws<ArgumentException>(() => PracticePackageBuilder.Create(manifest, unit, new SpeakingBlock { BlockId = "fake", ExerciseId = "fake", Prompt = "fake" }, rubric, config, vocabulary, "en-US", "guided", 10));
    }

    [Fact]
    public void WritingRubricKeepsOriginalWeightsAndNoAutomaticPronunciationClaim()
    {
        var (manifest, unit, assessments, vocabulary, config) = Source();
        var task = unit.Sections.SelectMany(s => s.Blocks).OfType<WritingBlock>().First();
        var rubric = assessments.Rubrics.Single(r => r.UnitId == unit.UnitId && r.Skill == "writing");
        var request = PracticePackageBuilder.Create(manifest, unit, task, rubric, config, vocabulary, "vi-VN", "challenge", 20);
        Assert.Equal(new[] { 40, 35, 25 }, request.AssessmentCriteria.Select(c => c.MaxPoints));
        Assert.Contains(request.RequiredEvidence, e => e.Contains("NOT ASSESSED"));
        Assert.Equal("writing", request.Skill);
    }

    [Fact]
    public void GenericExporterUsesManifestLanguageAndRejectsUnscoredListening()
    {
        var (manifest, unit, assessments, vocabulary, config) = Source();
        var speaking = unit.Sections.SelectMany(s => s.Blocks).OfType<SpeakingBlock>().First();
        var listening = unit.Sections.SelectMany(s => s.Blocks).OfType<ListeningBlock>().First();
        var rubric = assessments.Rubrics.Single(r => r.UnitId == unit.UnitId && r.Skill.Contains("speaking"));
        var japanese = manifest with { CourseId = "japanese-course", LanguageCode = "ja" };
        var japaneseUnit = unit with { CourseId = japanese.CourseId };
        var japaneseRubric = rubric with { UnitId = japaneseUnit.UnitId };
        var request = PracticePackageBuilder.Create(japanese, japaneseUnit, speaking, japaneseRubric, config with { CourseId = japanese.CourseId }, vocabulary with { CourseId = japanese.CourseId }, "vi-VN", "standard", 15);
        Assert.Equal("ja", request.TargetLanguage);
        Assert.Throws<ArgumentException>(() => PracticePackageBuilder.Create(manifest, unit, listening, rubric, config, vocabulary, "vi-VN", "standard", 15));
    }
}
