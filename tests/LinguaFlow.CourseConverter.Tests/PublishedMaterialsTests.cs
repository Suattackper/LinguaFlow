using System.IO.Compression;
using System.Text.Json;
using LinguaFlow.Core.Courses;
using LinguaFlow.CourseConverter;

namespace LinguaFlow.CourseConverter.Tests;

public sealed class PublishedMaterialsTests
{
    private static readonly string Root = FindRoot();
    private static readonly string CourseRoot = Path.Combine(Root, "src", "LinguaFlow.Web", "wwwroot", "Courses", "English", "english-life-office-it");

    [Fact]
    public async Task PublishedResourcesExistMatchManifestAndResolveUnderProjectSubpath()
    {
        var manifest = JsonSerializer.Deserialize<CourseManifest>(await File.ReadAllTextAsync(Path.Combine(CourseRoot, "manifest.json")), CourseJson.Options)!;
        Assert.Equal(3, manifest.Downloads.Count);
        var resolver = new CoursePathResolver();
        var manifestUrl = new Uri("https://example.org/LinguaFlow/Courses/English/english-life-office-it/manifest.json");
        foreach (var item in manifest.Downloads)
        {
            Assert.True(item.Available);
            Assert.StartsWith("https://example.org/LinguaFlow/Courses/English/english-life-office-it/Downloads/", resolver.ResolveContent(manifestUrl, item.Path).AbsoluteUri);
            var bytes = await File.ReadAllBytesAsync(Path.Combine(CourseRoot, item.Path.Replace('/', Path.DirectorySeparatorChar)));
            Assert.Equal(item.FileSize, bytes.LongLength);
            if (item.Format == "pdf") Assert.True(bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8));
        }
        await ConverterCli.ValidatePublishedAsync(Path.Combine(Root, "src", "LinguaFlow.Web", "wwwroot", "Courses"));
    }

    [Fact]
    public void CompletePackageContainsOnlyTheTwoApprovedOriginalPdfs()
    {
        using var archive = ZipFile.OpenRead(Path.Combine(CourseRoot, "Downloads", "EnglishForLifeOfficeIT_CompleteCourse.zip"));
        Assert.Equal(["AnswerKey_Units01-36.pdf", "StudentBook_Units01-36.pdf"], archive.Entries.Select(x => x.FullName).OrderBy(x => x).ToArray());
        var source = Path.Combine(Root, "CourseSources", "English", "english-life-office-it", "PDF");
        foreach (var entry in archive.Entries)
        {
            using var stream = entry.Open(); using var buffer = new MemoryStream(); stream.CopyTo(buffer);
            Assert.Equal(File.ReadAllBytes(Path.Combine(source, entry.FullName)), buffer.ToArray());
        }
    }

    [Fact]
    public async Task PublisherRejectsUndeclaredFilesInPublicCourseTree()
    {
        var source = Path.Combine(Root, "src", "LinguaFlow.Web", "wwwroot", "Courses");
        var temp = Path.Combine(Path.GetTempPath(), "linguaflow-public-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                var target = Path.Combine(temp, Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target);
            }
            var stray = Path.Combine(temp, "English", "english-life-office-it", "Downloads", "private-tracking.json");
            await File.WriteAllTextAsync(stray, "{}");
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => ConverterCli.ValidatePublishedAsync(temp));
            Assert.Contains("Undeclared public course file", error.Message);
        }
        finally
        {
            var target = Path.GetFullPath(temp);
            var safeRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!target.StartsWith(safeRoot, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(target).StartsWith("linguaflow-public-test-", StringComparison.Ordinal)) throw new InvalidOperationException("Unsafe temporary cleanup path.");
            if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
        }
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "LinguaFlow.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
