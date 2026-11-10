using System.Text.RegularExpressions;

namespace LinguaFlow.CourseConverter;

public sealed record CourseSources(
    string Directory,
    string Curriculum,
    string StudentBook,
    string AnswerKeyBook,
    string VocabularyBank,
    string TeacherGuide,
    IReadOnlyDictionary<int, string> Lessons,
    IReadOnlyDictionary<int, string> AnswerKeys,
    IReadOnlyList<string> AllFiles);

public static class SourceDiscovery
{
    public static CourseSources Discover(string englishRoot)
    {
        var files = Directory.EnumerateFiles(englishRoot, "*", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var curriculumFiles = files.Where(x => Path.GetFileName(x).Equals("CURRICULUM.md", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (curriculumFiles.Length != 1) throw new InvalidOperationException($"Expected one curriculum under {englishRoot}; found {curriculumFiles.Length}.");
        var courseDirectory = Path.GetDirectoryName(curriculumFiles[0])!;
        var courseFiles = files.Where(x => Path.GetFullPath(x).StartsWith(Path.GetFullPath(courseDirectory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)).ToArray();
        string Required(string name) => courseFiles.SingleOrDefault(x => Path.GetFileName(x).Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException($"Required source file not found: {name}");
        var lessons = NumberedFiles(courseFiles, @"^Unit_(\d+)\.md$", "LESSONS");
        var answers = NumberedFiles(courseFiles, @"^Unit_(\d+)_Answers\.md$", "ANSWER_KEYS");
        if (lessons.Count == 0) throw new InvalidOperationException("No individual lesson files were discovered.");
        return new CourseSources(courseDirectory, curriculumFiles[0], Required("STUDENT_BOOK.md"), Required("ANSWER_KEY_BOOK.md"), Required("VOCABULARY_BANK.md"), Required("GPT-TEACHER.md"), lessons, answers, courseFiles);
    }

    private static IReadOnlyDictionary<int, string> NumberedFiles(IEnumerable<string> files, string pattern, string expectedParent)
    {
        var result = new Dictionary<int, string>();
        foreach (var path in files.Where(x => string.Equals(Path.GetFileName(Path.GetDirectoryName(x)), expectedParent, StringComparison.OrdinalIgnoreCase)))
        {
            var match = Regex.Match(Path.GetFileName(path), pattern, RegexOptions.IgnoreCase);
            if (!match.Success) continue;
            var number = int.Parse(match.Groups[1].Value);
            if (!result.TryAdd(number, path)) throw new InvalidOperationException($"Duplicate Unit {number:00} source files in {expectedParent}.");
        }
        return result;
    }
}
