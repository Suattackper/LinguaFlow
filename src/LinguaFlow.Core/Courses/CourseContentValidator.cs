using System.Text.RegularExpressions;

namespace LinguaFlow.Core.Courses;

public sealed class CourseContentValidator
{
    private static readonly Regex IdPattern = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);
    private static readonly Regex FingerprintPattern = new("^sha256:[0-9a-f]{64}$", RegexOptions.Compiled);

    public IReadOnlyList<string> ValidateCatalog(CourseCatalog? catalog)
    {
        var errors = new List<string>();
        if (catalog is null) return ["Catalog is null."];
        if (catalog.SchemaVersion != 1) errors.Add("Unsupported catalog schema version.");
        if (catalog.Courses is null) return [.. errors, "Catalog courses are missing."];
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in catalog.Courses)
        {
            if (!ValidId(entry.CourseId) || !ids.Add(entry.CourseId)) errors.Add($"Invalid or duplicate courseId: {entry.CourseId}");
            string expected;
            try { expected = CoursePathResolver.LanguageDirectory(entry.LanguageCode); }
            catch (CourseContentException) { errors.Add($"Unsupported language code: {entry.LanguageCode}"); continue; }
            if (entry.LanguageDirectory != expected) errors.Add($"Inconsistent language directory: {entry.CourseId}");
            if (!Enum.IsDefined(entry.Status)) errors.Add($"Invalid course status: {entry.CourseId}");
            if (string.IsNullOrWhiteSpace(entry.Title) || string.IsNullOrWhiteSpace(entry.Description) || string.IsNullOrWhiteSpace(entry.Level)) errors.Add($"Missing course metadata: {entry.CourseId}");
            if (!CoursePathResolver.IsSafeRelativePath(entry.ManifestPath) || entry.ManifestPath != CoursePathResolver.CourseRoot(entry) + "manifest.json") errors.Add($"Invalid manifest path: {entry.CourseId}");
            if (entry.CoverImage is not null && (!CoursePathResolver.IsSafeRelativePath(entry.CoverImage) || !entry.CoverImage.StartsWith(CoursePathResolver.CourseRoot(entry) + "Assets/", StringComparison.Ordinal))) errors.Add($"Invalid cover image path: {entry.CourseId}");
        }
        return errors;
    }

    public IReadOnlyList<string> ValidateManifest(CourseManifest? manifest, CourseCatalogEntry entry)
    {
        var errors = new List<string>();
        if (manifest is null) return ["Manifest is null."];
        if (manifest.SchemaVersion != 1) errors.Add("Unsupported manifest schema version.");
        if (manifest.CourseId != entry.CourseId || manifest.LanguageCode != entry.LanguageCode) errors.Add("Manifest course/language reference does not match catalog.");
        if (string.IsNullOrWhiteSpace(manifest.ContentVersion) || !FingerprintPattern.IsMatch(manifest.ContentFingerprint ?? "")) errors.Add("Invalid content version or fingerprint.");
        if (string.IsNullOrWhiteSpace(manifest.Title) || string.IsNullOrWhiteSpace(manifest.Subtitle) || string.IsNullOrWhiteSpace(manifest.Description) || string.IsNullOrWhiteSpace(manifest.LearningLevel)) errors.Add("Missing manifest metadata.");
        if (manifest.Units is null || manifest.Units.Count == 0) errors.Add("Manifest must reference at least one unit.");
        else
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var orders = new HashSet<int>();
            foreach (var unit in manifest.Units)
            {
                if (!ValidId(unit.UnitId) || !ids.Add(unit.UnitId)) errors.Add($"Invalid or duplicate unitId: {unit.UnitId}");
                if (unit.Order <= 0 || !orders.Add(unit.Order)) errors.Add($"Invalid or duplicate unit order: {unit.UnitId}");
                if (string.IsNullOrWhiteSpace(unit.Title) || !ValidReference(unit.Path, "Units/")) errors.Add($"Invalid unit reference: {unit.UnitId}");
            }
        }
        ValidateRefs(manifest.Vocabulary, "Vocabulary/", "vocabulary", errors);
        ValidateRefs(manifest.Assessments, "Assessments/", "assessment", errors);
        ValidateRefs(manifest.AiPractice, "Prompts/", "AI practice", errors);
        if (manifest.MediaAssets is not null) ValidateRefs(manifest.MediaAssets, "Assets/", "media", errors);
        if (manifest.Downloads is null) errors.Add("Download references are missing.");
        else
        {
            var downloadIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var download in manifest.Downloads)
                if (!ValidId(download.Id) || !downloadIds.Add(download.Id) || string.IsNullOrWhiteSpace(download.Title) || string.IsNullOrWhiteSpace(download.Description) ||
                    !ValidFileReference(download.Path, "Downloads/") || download.Category is not ("student-book" or "answer-key" or "supplementary" or "full-package") ||
                    download.Format is not ("pdf" or "md" or "zip") || download.FileSize is <= 0 || download.DisplayOrder < 0 ||
                    download.SourceLanguage != manifest.LanguageCode)
                    errors.Add($"Invalid or duplicate download reference: {download.Id}");
        }
        if (manifest.CompletionRules is null || manifest.CompletionRules.Kind is not ("all-units" or "minimum-units") || (manifest.CompletionRules.Kind == "minimum-units" && (manifest.CompletionRules.MinimumCompletedUnits is null or <= 0 || manifest.CompletionRules.MinimumCompletedUnits > manifest.Units?.Count))) errors.Add("Invalid completion rules.");
        return errors;
    }

    public IReadOnlyList<string> ValidateUnit(UnitContent? unit, CourseManifest manifest, string expectedUnitId)
    {
        var errors = new List<string>();
        if (unit is null) return ["Unit is null."];
        if (unit.SchemaVersion != 1) errors.Add("Unsupported unit schema version.");
        if (unit.CourseId != manifest.CourseId || unit.UnitId != expectedUnitId || !manifest.Units.Any(x => x.UnitId == unit.UnitId)) errors.Add("Incorrect unit/course reference.");
        if (string.IsNullOrWhiteSpace(unit.Title) || unit.Sections is null || unit.Sections.Count == 0) errors.Add("Unit title or sections missing.");
        ValidateSource(unit.Source, errors, "unit");
        if (unit.Sections is null) return errors;
        var sectionIds = new HashSet<string>();
        var blockIds = new HashSet<string>();
        var exerciseIds = new HashSet<string>();
        foreach (var section in unit.Sections)
        {
            if (!ValidId(section.SectionId) || !sectionIds.Add(section.SectionId)) errors.Add($"Invalid or duplicate sectionId: {section.SectionId}");
            ValidateSource(section.Source, errors, $"section {section.SectionId}");
            if (string.IsNullOrWhiteSpace(section.Title) || section.Blocks is null) { errors.Add($"Invalid section: {section.SectionId}"); continue; }
            foreach (var block in section.Blocks)
            {
                if (!ValidId(block.BlockId) || !blockIds.Add(block.BlockId)) errors.Add($"Invalid or duplicate blockId: {block.BlockId}");
                ValidateSource(block.Source, errors, $"block {block.BlockId}");
                var exerciseId = block switch { QuestionBlock q => q.ExerciseId, ExerciseGroupBlock g => g.ExerciseId, WritingBlock w => w.ExerciseId, SpeakingBlock s => s.ExerciseId, ListeningBlock l => l.ExerciseId, _ => null };
                if (exerciseId is not null && (!ValidId(exerciseId) || !exerciseIds.Add(exerciseId))) errors.Add($"Invalid or duplicate exerciseId in block: {block.BlockId}");
                if (block is ImageBlock image && (!ValidFileReference(image.Path, "Assets/") || string.IsNullOrWhiteSpace(image.Alt)) || block is AudioBlock audio && !ValidFileReference(audio.Path, "Assets/")) errors.Add($"Invalid media block: {block.BlockId}");
                var requiredText = block switch { HeadingBlock b => b.Text, ExplanationBlock b => b.Text, GrammarBlock b => b.Text, ExampleBlock b => b.Text, DialogueBlock b => b.Text, ReadingBlock b => b.Text, VocabularyBlock b => b.Text, HomeworkBlock b => b.Text, ReviewBlock b => b.Text, QuestionBlock b => b.Prompt, ExerciseGroupBlock b => b.Title, WritingBlock b => b.Prompt, SpeakingBlock b => b.Prompt, ListeningBlock b => b.Prompt, _ => null };
                if (requiredText is not null && string.IsNullOrWhiteSpace(requiredText)) errors.Add($"Missing block content: {block.BlockId}");
                if (block.LocalizedText is not null && block.LocalizedText.Any(kv => string.IsNullOrWhiteSpace(kv.Key) || string.IsNullOrWhiteSpace(kv.Value))) errors.Add($"Invalid localized text: {block.BlockId}");
            }
        }
        return errors;
    }

    public IReadOnlyList<string> ValidateVocabulary(VocabularyDocument? document, CourseManifest manifest)
    {
        var errors = new List<string>();
        if (document is null) return ["Vocabulary document is null."];
        if (document.SchemaVersion != 1 || document.CourseId != manifest.CourseId) errors.Add("Invalid vocabulary schema or course reference.");
        if (document.Entries is null) return [.. errors, "Vocabulary entries missing."];
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in document.Entries)
        {
            if (!ValidId(entry.VocabularyId) || !ids.Add(entry.VocabularyId)) errors.Add($"Invalid or duplicate vocabularyId: {entry.VocabularyId}");
            if (!manifest.Units.Any(x => x.UnitId == entry.UnitId)) errors.Add($"Vocabulary references unknown unit: {entry.UnitId}");
            if (string.IsNullOrWhiteSpace(entry.Term) || string.IsNullOrWhiteSpace(entry.Vietnamese)) errors.Add($"Incomplete vocabulary entry: {entry.VocabularyId}");
            if (entry.AudioPath is not null && (!CoursePathResolver.IsSafeRelativePath(entry.AudioPath) || !entry.AudioPath.StartsWith("Assets/", StringComparison.Ordinal))) errors.Add($"Invalid vocabulary audio path: {entry.VocabularyId}");
            ValidateSource(entry.Source, errors, $"vocabulary {entry.VocabularyId}");
        }
        return errors;
    }

    public IReadOnlyList<string> ValidateAssessments(AssessmentDocument? document, CourseManifest manifest, IEnumerable<UnitContent> units)
    {
        var errors = new List<string>();
        if (document is null) return ["Assessment document is null."];
        if (document.SchemaVersion != 1 || document.CourseId != manifest.CourseId) errors.Add("Invalid assessment schema or course reference.");
        if (document.Answers is null || document.Rubrics is null || document.Thresholds is null) return [.. errors, "Assessment collections missing."];
        var questions = units.SelectMany(x => x.Sections).SelectMany(x => x.Blocks).OfType<QuestionBlock>().Select(x => x.ExerciseId).ToHashSet(StringComparer.Ordinal);
        var answerIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var answer in document.Answers)
        {
            if (!questions.Contains(answer.ExerciseId) || !answerIds.Add(answer.ExerciseId)) errors.Add($"Invalid or duplicate answer mapping: {answer.ExerciseId}");
            if (!manifest.Units.Any(x => x.UnitId == answer.UnitId) || string.IsNullOrWhiteSpace(answer.Answer) || answer.AssessmentMode is not ("auto-choice" or "auto-exact" or "manual-review")) errors.Add($"Invalid answer metadata: {answer.ExerciseId}");
            ValidateSource(answer.Source, errors, $"answer {answer.ExerciseId}");
        }
        foreach (var rubric in document.Rubrics)
        {
            if (!ValidId(rubric.RubricId) || !manifest.Units.Any(x => x.UnitId == rubric.UnitId) || string.IsNullOrWhiteSpace(rubric.Text)) errors.Add($"Invalid rubric: {rubric.RubricId}");
            ValidateSource(rubric.Source, errors, $"rubric {rubric.RubricId}");
        }
        return errors;
    }

    public IReadOnlyList<string> ValidatePractice(PracticeConfiguration? document, CourseManifest manifest)
    {
        var errors = new List<string>();
        if (document is null) return ["Practice configuration is null."];
        if (document.SchemaVersion != 1 || document.CourseId != manifest.CourseId || string.IsNullOrWhiteSpace(document.Instructions) || document.Modes is null || document.Modes.Count == 0) errors.Add("Invalid practice configuration.");
        ValidateSource(document.Source, errors, "practice configuration");
        return errors;
    }

    private static bool ValidId(string? id) => id is not null && IdPattern.IsMatch(id);
    private static void ValidateSource(SourceProvenance? source, List<string> errors, string label)
    {
        if (source is null) return;
        if (!CoursePathResolver.IsSafeRelativePath(source.File) || string.IsNullOrWhiteSpace(source.OriginalUnit) || string.IsNullOrWhiteSpace(source.OriginalSection)) errors.Add($"Invalid source provenance: {label}");
    }
    private static bool ValidReference(string? path, string folder) => CoursePathResolver.IsSafeRelativePath(path) && path!.StartsWith(folder, StringComparison.Ordinal) && path.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
    private static bool ValidFileReference(string? path, string folder) => CoursePathResolver.IsSafeRelativePath(path) && path!.StartsWith(folder, StringComparison.Ordinal) && path.Length > folder.Length;
    private static void ValidateRefs(List<ContentReference>? refs, string folder, string label, List<string> errors)
    {
        if (refs is null) { errors.Add($"{label} references are missing."); return; }
        var ids = new HashSet<string>();
        foreach (var item in refs) if (!ValidId(item.Id) || !ids.Add(item.Id) || !ValidReference(item.Path, folder)) errors.Add($"Invalid or duplicate {label} reference: {item.Id}");
    }
}
