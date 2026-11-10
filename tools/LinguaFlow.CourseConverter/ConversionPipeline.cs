using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LinguaFlow.Core.Courses;

namespace LinguaFlow.CourseConverter;

public sealed record ConversionPlan(
    CourseCatalog Catalog,
    CourseManifest Manifest,
    IReadOnlyList<UnitContent> Units,
    VocabularyDocument Vocabulary,
    AssessmentDocument Assessments,
    PracticeConfiguration Practice,
    IReadOnlyDictionary<string, byte[]> Files,
    IReadOnlyList<string> Warnings,
    string Report);

public static class ConversionPipeline
{
    public static async Task<ConversionPlan> BuildAsync(string englishRoot, CancellationToken cancellationToken = default)
    {
        var sources = SourceDiscovery.Discover(englishRoot);
        var courseId = Path.GetFileName(sources.Directory.TrimEnd(Path.DirectorySeparatorChar));
        var warnings = new List<string>();
        var studentBook = await File.ReadAllTextAsync(sources.StudentBook, cancellationToken);
        var answerBook = await File.ReadAllTextAsync(sources.AnswerKeyBook, cancellationToken);
        var curriculum = await File.ReadAllTextAsync(sources.Curriculum, cancellationToken);
        var teacherGuide = await File.ReadAllTextAsync(sources.TeacherGuide, cancellationToken);
        var bookUnits = CombinedUnits(studentBook, @"^# UNIT\s+(\d+)\s+[—–-]");
        var bookAnswers = CombinedUnits(answerBook, @"^# ANSWER KEY\s+[—–-]\s+UNIT\s+(\d+):");
        var curriculumTitles = CurriculumTitles(curriculum);
        var unitNumbers = sources.Lessons.Keys.Union(bookUnits.Keys).OrderBy(x => x).ToArray();
        if (unitNumbers.Length == 0) throw new InvalidOperationException("No Unit boundaries were found.");
        if (unitNumbers.Length != 36) warnings.Add($"Discovered {unitNumbers.Length} Units, while the source curriculum describes 36.");
        for (var n = 1; n <= unitNumbers.Max(); n++) if (!unitNumbers.Contains(n)) warnings.Add($"Unit {n:00} is absent from both individual lessons and combined Student Book.");

        var vocabulary = VocabularyParser.Parse(courseId, await File.ReadAllTextAsync(sources.VocabularyBank, cancellationToken), warnings);
        var units = new List<UnitContent>();
        var answers = new List<AnswerRecord>();
        var rubrics = new List<RubricRecord>();
        var thresholds = new List<ThresholdRule>();
        foreach (var number in unitNumbers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sourceFile = sources.Lessons.TryGetValue(number, out var lessonPath) ? $"LESSONS/Unit_{number:00}.md" : "STUDENT_BOOK.md";
            if (lessonPath is null) warnings.Add($"Unit {number:00}: individual lesson missing; used Student Book section.");
            var markdown = lessonPath is not null ? await File.ReadAllTextAsync(lessonPath, cancellationToken) : bookUnits[number];
            if (lessonPath is not null && bookUnits.TryGetValue(number, out var bookUnit) && Normalize(markdown) != Normalize(bookUnit)) warnings.Add($"Unit {number:00}: individual lesson differs from combined Student Book; individual lesson retained.");
            if (!bookUnits.ContainsKey(number)) warnings.Add($"Unit {number:00}: absent from combined Student Book.");
            var parsed = UnitParser.Parse(number, courseId, markdown, sourceFile);
            warnings.AddRange(parsed.Warnings);
            var unit = AttachVocabulary(parsed.Content, vocabulary, warnings);
            if (curriculumTitles.TryGetValue(number, out var curriculumTitle) && curriculumTitle != unit.Title) warnings.Add($"Unit {number:00}: curriculum title '{curriculumTitle}' differs from lesson title '{unit.Title}'.");
            if (!curriculumTitles.ContainsKey(number)) warnings.Add($"Unit {number:00}: absent from Curriculum table.");
            units.Add(unit);
            thresholds.AddRange(ParseThresholds(unit));

            var answerSource = sources.AnswerKeys.TryGetValue(number, out var answerPath) ? $"ANSWER_KEYS/Unit_{number:00}_Answers.md" : "ANSWER_KEY_BOOK.md";
            if (answerPath is null && !bookAnswers.ContainsKey(number)) { warnings.Add($"Unit {number:00}: no answer key found."); continue; }
            if (answerPath is null) warnings.Add($"Unit {number:00}: individual answer key missing; used combined Answer Key Book.");
            var answerMarkdown = answerPath is not null ? await File.ReadAllTextAsync(answerPath, cancellationToken) : bookAnswers[number];
            if (answerPath is not null && bookAnswers.TryGetValue(number, out var bookAnswer) && Normalize(answerMarkdown) != Normalize(bookAnswer)) warnings.Add($"Unit {number:00}: individual answer key differs from combined Answer Key Book; individual key retained.");
            if (!bookAnswers.ContainsKey(number)) warnings.Add($"Unit {number:00}: absent from combined Answer Key Book.");
            var parsedAnswers = AnswerKeyParser.Parse(number, answerMarkdown, unit, answerSource);
            answers.AddRange(parsedAnswers.Answers);
            rubrics.AddRange(parsedAnswers.Rubrics);
            warnings.AddRange(parsedAnswers.Warnings);
        }
        var assessmentDocument = new AssessmentDocument { SchemaVersion = 1, CourseId = courseId, Answers = answers, Rubrics = rubrics, Thresholds = thresholds };
        var practice = new PracticeConfiguration
        {
            SchemaVersion = 1, CourseId = courseId, Instructions = teacherGuide,
            Modes = ExtractModes(teacherGuide), AudioEvidenceRequiredForListening = true, AudioEvidenceRequiredForPronunciation = true,
            Source = new SourceProvenance { File = "GPT-TEACHER.md", OriginalUnit = "all", OriginalSection = "Teaching protocol" }
        };
        var options = new JsonSerializerOptions(CourseJson.Options) { WriteIndented = true };
        var payloads = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var unit in units) payloads[$"Units/{unit.UnitId}.json"] = JsonSerializer.SerializeToUtf8Bytes(unit, options);
        payloads["Vocabulary/vocabulary.json"] = JsonSerializer.SerializeToUtf8Bytes(vocabulary, options);
        payloads["Assessments/assessments.json"] = JsonSerializer.SerializeToUtf8Bytes(assessmentDocument, options);
        payloads["Prompts/practice-config.json"] = JsonSerializer.SerializeToUtf8Bytes(practice, options);
        var title = MarkdownStructure.Split(studentBook).First().Heading;
        var description = "Bộ giáo trình riêng cho người học Việt Nam làm công việc phần mềm.";
        var manifest = new CourseManifest
        {
            SchemaVersion = 1, CourseId = courseId, ContentVersion = "1.1.0", ContentFingerprint = ContentFingerprint.Compute(payloads.Select(x => (x.Key, x.Value))), LanguageCode = "en",
            Title = title, Subtitle = "A2+ → B2", Description = description, LearningLevel = "A2+ → B2",
            Units = units.Select((x, index) => new UnitReference { UnitId = x.UnitId, Title = x.Title, Order = index + 1, Path = $"Units/{x.UnitId}.json" }).ToList(),
            Vocabulary = [new ContentReference { Id = "vocabulary", Path = "Vocabulary/vocabulary.json" }],
            Assessments = [new ContentReference { Id = "assessments", Path = "Assessments/assessments.json" }],
            AiPractice = [new ContentReference { Id = "practice-config", Path = "Prompts/practice-config.json" }],
            Downloads = [], MediaAssets = [], CompletionRules = new CompletionRules { Kind = "all-units" }
        };
        var entry = new CourseCatalogEntry
        {
            CourseId = courseId, LanguageCode = "en", LanguageDirectory = "English", Title = title, Description = description,
            Level = "A2+ → B2", Status = CourseStatus.Available, ManifestPath = $"Courses/English/{courseId}/manifest.json",
            Tags = ["English", "Office", "IT"]
        };
        var catalog = new CourseCatalog { SchemaVersion = 1, Courses = [entry] };
        var validation = Validate(catalog, manifest, units, vocabulary, assessmentDocument, practice);
        if (validation.Count > 0) throw new InvalidOperationException("Generated LCF is invalid: " + string.Join(" | ", validation));
        var files = new SortedDictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["catalog.json"] = JsonSerializer.SerializeToUtf8Bytes(catalog, options),
            [$"English/{courseId}/manifest.json"] = JsonSerializer.SerializeToUtf8Bytes(manifest, options)
        };
        foreach (var payload in payloads) files[$"English/{courseId}/{payload.Key}"] = payload.Value;
        var report = ConversionReport.Create(sources, bookUnits.Count, bookAnswers.Count, curriculumTitles.Count, units, vocabulary, assessmentDocument, warnings, manifest.ContentFingerprint);
        return new ConversionPlan(catalog, manifest, units, vocabulary, assessmentDocument, practice, files, warnings, report);
    }

    public static IReadOnlyList<string> Validate(CourseCatalog catalog, CourseManifest manifest, IReadOnlyList<UnitContent> units, VocabularyDocument vocabulary, AssessmentDocument assessments, PracticeConfiguration practice)
    {
        var validator = new CourseContentValidator();
        var entry = catalog.Courses.Single();
        var errors = new List<string>();
        errors.AddRange(validator.ValidateCatalog(catalog));
        errors.AddRange(validator.ValidateManifest(manifest, entry));
        foreach (var unit in units) errors.AddRange(validator.ValidateUnit(unit, manifest, unit.UnitId));
        errors.AddRange(validator.ValidateVocabulary(vocabulary, manifest));
        errors.AddRange(validator.ValidateAssessments(assessments, manifest, units));
        errors.AddRange(validator.ValidatePractice(practice, manifest));
        if (manifest.Units.Count != units.Count || manifest.Units.Any(x => units.All(u => u.UnitId != x.UnitId))) errors.Add("Manifest Unit references do not match generated Units.");
        return errors;
    }

    private static UnitContent AttachVocabulary(UnitContent unit, VocabularyDocument document, List<string> warnings)
    {
        var bankEntries = document.Entries.Where(x => x.UnitId == unit.UnitId).ToList();
        var section = unit.Sections.FirstOrDefault(x => x.SectionId == "a");
        if (section is null) { warnings.Add($"{unit.UnitId}: vocabulary section missing."); return unit; }
        var lessonEntries = new List<VocabularyEntry>();
        foreach (var line in section.SourceMarkdown?.Split('\n') ?? [])
        {
            var cells = line.Trim().Trim('|').Split('|').Select(x => x.Trim()).ToArray();
            if (!line.TrimStart().StartsWith('|') || cells.Length < 4 || !int.TryParse(cells[0], out var index)) continue;
            var termMatch = Regex.Match(cells[1], @"\*\*(.+?)\*\*\s*(.*)");
            if (!termMatch.Success) { warnings.Add($"{unit.UnitId}: could not parse vocabulary row {index}."); continue; }
            lessonEntries.Add(new VocabularyEntry
            {
                VocabularyId = $"{unit.UnitId}-vocab-{index:00}", UnitId = unit.UnitId,
                Term = termMatch.Groups[1].Value, Ipa = termMatch.Groups[2].Value, Vietnamese = cells[2], Context = cells[3],
                Source = section.Source!
            });
        }
        if (lessonEntries.Count != bankEntries.Count) warnings.Add($"{unit.UnitId}: lesson vocabulary has {lessonEntries.Count} parsed rows but bank has {bankEntries.Count}.");
        foreach (var (lesson, bank) in lessonEntries.Zip(bankEntries))
            if (!string.Equals(lesson.Term, bank.Term, StringComparison.OrdinalIgnoreCase)) warnings.Add($"{unit.UnitId}: lesson vocabulary '{lesson.Term}' differs from bank '{bank.Term}' at {lesson.VocabularyId}; lesson retained in Unit.");
        var sections = unit.Sections.Select(x => x.SectionId != "a" ? x : x with { Blocks = x.Blocks.Select(b => b is VocabularyBlock v ? v with { Entries = lessonEntries } : b).ToList() }).ToList();
        return unit with { Sections = sections };
    }

    private static List<ThresholdRule> ParseThresholds(UnitContent unit)
    {
        var h = unit.Sections.FirstOrDefault(x => x.SectionId == "h");
        if (h is null) return [];
        var rules = new List<ThresholdRule>();
        foreach (var line in h.SourceMarkdown!.Split('\n').Select(x => x.Trim()).Where(x => x.StartsWith("- ") || x.Contains("≥")))
        {
            var skill = line.Contains("Grammar", StringComparison.OrdinalIgnoreCase) ? "grammar" : line.Contains("Vocabulary", StringComparison.OrdinalIgnoreCase) ? "vocabulary" : line.Contains("Reading", StringComparison.OrdinalIgnoreCase) ? "reading" : "progression";
            var score = Regex.Match(line, @"≥\s*(\d+)\s*/\s*(\d+)");
            var percent = Regex.Match(line, @"≥\s*(\d+)%");
            rules.Add(new ThresholdRule
            {
                UnitId = unit.UnitId, Skill = skill, Text = line,
                MinimumScore = score.Success ? int.Parse(score.Groups[1].Value) : null,
                TotalScore = score.Success ? int.Parse(score.Groups[2].Value) : null,
                MinimumPercent = percent.Success ? int.Parse(percent.Groups[1].Value) : null
            });
        }
        return rules;
    }

    private static Dictionary<int, string> CombinedUnits(string book, string headingPattern)
    {
        var matches = Regex.Matches(book, headingPattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
        var result = new Dictionary<int, string>();
        for (var i = 0; i < matches.Count; i++)
        {
            var number = int.Parse(matches[i].Groups[1].Value);
            var end = i + 1 < matches.Count ? matches[i + 1].Index : book.Length;
            if (!result.TryAdd(number, book[matches[i].Index..end].Trim())) throw new InvalidOperationException($"Combined book has duplicate Unit {number:00}.");
        }
        return result;
    }

    private static Dictionary<int, string> CurriculumTitles(string curriculum)
    {
        var result = new Dictionary<int, string>();
        foreach (var line in curriculum.Split('\n'))
        {
            var match = Regex.Match(line, @"^\|\s*(\d{2})\s*\|\s*\*\*(.+?)\*\*", RegexOptions.IgnoreCase);
            if (match.Success) result[int.Parse(match.Groups[1].Value)] = match.Groups[2].Value.Trim();
        }
        return result;
    }

    private static List<string> ExtractModes(string teacherGuide)
    {
        var modes = new List<string>();
        if (teacherGuide.Contains("TEXT MODE", StringComparison.OrdinalIgnoreCase)) modes.Add("text");
        if (teacherGuide.Contains("VOICE TEACHER MODE", StringComparison.OrdinalIgnoreCase)) modes.Add("voice-teacher");
        if (teacherGuide.Contains("VOICE PARTNER MODE", StringComparison.OrdinalIgnoreCase)) modes.Add("voice-partner");
        if (teacherGuide.Contains("MIXED", StringComparison.OrdinalIgnoreCase)) modes.Add("mixed");
        return modes;
    }

    private static string Normalize(string value) => Regex.Replace(value.Replace("\r\n", "\n").Trim(), @"(?:\n\s*---\s*)+$", "").Trim();
}
