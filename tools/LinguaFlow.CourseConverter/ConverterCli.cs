using System.Text.Json;
using System.IO.Compression;
using LinguaFlow.Core.Courses;

namespace LinguaFlow.CourseConverter;

public static class ConverterCli
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0) return Usage();
        try
        {
            if (args[0] == "validate" && args.Length == 2)
            {
                await ValidatePublishedAsync(Path.GetFullPath(args[1]));
                Console.WriteLine("Published LCF content is valid.");
                return 0;
            }
            if (args[0] == "publish-materials" && args.Length == 3)
            {
                await PublishMaterialsAsync(Path.GetFullPath(args[1]), Path.GetFullPath(args[2]));
                await ValidatePublishedAsync(Path.GetFullPath(args[2]));
                return 0;
            }
            if (args[0] == "validate-publish" && args.Length == 3)
            {
                await ValidatePublishAsync(Path.GetFullPath(args[1]), args[2]);
                Console.WriteLine("Published static site is valid.");
                return 0;
            }
            if (args[0] == "report" && args.Length == 3)
            {
                var root = Path.GetFullPath(args[1]);
                var reportPath = Path.GetFullPath(args[2]);
                EnsureOutsideSource(root, reportPath);
                var plan = await ConversionPipeline.BuildAsync(root);
                Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
                await File.WriteAllTextAsync(reportPath, plan.Report);
                Console.WriteLine($"Report written: {reportPath}");
                return 0;
            }
            if (args[0] is "convert" or "rebuild" && args.Length == 4)
                return await ConvertAsync(args[1], args[2], args[3]);
            // Preserve the two-argument Step 02 command while publishing the richer Step 03 format.
            if (args.Length == 2 && Directory.Exists(args[0]))
            {
                var root = File.Exists(Path.Combine(args[0], "CURRICULUM.md")) ? Path.GetDirectoryName(Path.GetFullPath(args[0]))! : args[0];
                return await ConvertAsync(root, args[1], Path.Combine("docs", "COURSE_CONVERSION_REPORT.md"));
            }
            return Usage();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static async Task<int> ConvertAsync(string englishRootArg, string coursesRootArg, string reportArg)
    {
        var englishRoot = Path.GetFullPath(englishRootArg);
        var coursesRoot = Path.GetFullPath(coursesRootArg);
        var reportPath = Path.GetFullPath(reportArg);
        EnsureOutsideSource(englishRoot, coursesRoot);
        EnsureOutsideSource(englishRoot, reportPath);
        var plan = await ConversionPipeline.BuildAsync(englishRoot);
        // All generated records are validated in memory before any published file is changed.
        foreach (var (relative, bytes) in plan.Files)
        {
            var target = Path.GetFullPath(Path.Combine(coursesRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
            if (!target.StartsWith(coursesRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Generated path escaped Courses directory.");
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            await File.WriteAllBytesAsync(target, bytes);
        }
        Directory.CreateDirectory(Path.Combine(coursesRoot, "Japan"));
        Directory.CreateDirectory(Path.Combine(coursesRoot, "China"));
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        await File.WriteAllTextAsync(reportPath, plan.Report);
        await PublishMaterialsAsync(englishRoot, coursesRoot);
        await ValidatePublishedAsync(coursesRoot);
        Console.WriteLine($"Converted {plan.Units.Count} Units; {plan.Vocabulary.Entries.Count} vocabulary entries; {plan.Assessments.Answers.Count} matched answer items; {plan.Warnings.Count} warnings.");
        Console.WriteLine($"Fingerprint: {plan.Manifest.ContentFingerprint}");
        Console.WriteLine($"Report: {reportPath}");
        return 0;
    }

    public static async Task ValidatePublishedAsync(string coursesRoot, bool allowCompressedVariants = false)
    {
        var catalog = await ReadAsync<CourseCatalog>(Path.Combine(coursesRoot, "catalog.json"));
        var validator = new CourseContentValidator();
        var errors = new List<string>(validator.ValidateCatalog(catalog));
        if (errors.Count > 0) throw new InvalidOperationException(string.Join(" | ", errors));
        foreach (var entry in catalog.Courses.Where(x => x.Status == CourseStatus.Available))
        {
            var courseRoot = Path.Combine(coursesRoot, entry.LanguageDirectory, entry.CourseId);
            var manifest = await ReadAsync<CourseManifest>(Path.Combine(courseRoot, "manifest.json"));
            errors.AddRange(validator.ValidateManifest(manifest, entry));
            if (errors.Count > 0) continue;
            var units = new List<UnitContent>();
            var fingerprintFiles = new List<(string Path, byte[] Bytes)>();
            foreach (var unitRef in manifest.Units)
            {
                var path = SafeCourseFile(courseRoot, unitRef.Path);
                var bytes = await File.ReadAllBytesAsync(path);
                fingerprintFiles.Add((unitRef.Path, bytes));
                var unit = JsonSerializer.Deserialize<UnitContent>(bytes, CourseJson.Options) ?? throw new InvalidOperationException($"Empty Unit: {unitRef.Path}");
                units.Add(unit);
                errors.AddRange(validator.ValidateUnit(unit, manifest, unitRef.UnitId));
            }
            var vocabulary = await ReadReferencedAsync<VocabularyDocument>(courseRoot, manifest.Vocabulary, fingerprintFiles);
            var assessments = await ReadReferencedAsync<AssessmentDocument>(courseRoot, manifest.Assessments, fingerprintFiles);
            var practice = await ReadReferencedAsync<PracticeConfiguration>(courseRoot, manifest.AiPractice, fingerprintFiles);
            errors.AddRange(validator.ValidateVocabulary(vocabulary, manifest));
            errors.AddRange(validator.ValidateAssessments(assessments, manifest, units));
            errors.AddRange(validator.ValidatePractice(practice, manifest));
            foreach (var download in manifest.Downloads.Where(x => x.Available))
            {
                var file = SafeCourseFile(courseRoot, download.Path);
                if (!File.Exists(file)) { errors.Add($"Missing download: {download.Path}"); continue; }
                var bytes = await File.ReadAllBytesAsync(file);
                if (bytes.LongLength != download.FileSize) errors.Add($"Download size mismatch: {download.Path}");
                if (download.Format == "pdf" && (bytes.Length < 5 || !bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8))) errors.Add($"Invalid PDF: {download.Path}");
                if (download.Format == "zip")
                {
                    try { using var archive = ZipFile.OpenRead(file); if (archive.Entries.Count == 0 || archive.Entries.Any(x => x.Length == 0)) errors.Add($"Incomplete ZIP: {download.Path}"); }
                    catch (InvalidDataException) { errors.Add($"Invalid ZIP: {download.Path}"); }
                }
            }
            var declared = new HashSet<string>(StringComparer.Ordinal) { "manifest.json" };
            foreach (var path in manifest.Units.Select(x => x.Path).Concat(manifest.Vocabulary.Select(x => x.Path)).Concat(manifest.Assessments.Select(x => x.Path)).Concat(manifest.AiPractice.Select(x => x.Path)).Concat(manifest.MediaAssets?.Select(x => x.Path) ?? []).Concat(manifest.Downloads.Select(x => x.Path))) declared.Add(path);
            foreach (var path in Directory.EnumerateFiles(courseRoot, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(courseRoot, path).Replace('\\', '/');
                var compressedBase = relative.EndsWith(".br", StringComparison.Ordinal) ? relative[..^3] : relative.EndsWith(".gz", StringComparison.Ordinal) ? relative[..^3] : "";
                if (!relative.EndsWith("/.gitkeep", StringComparison.Ordinal) && !declared.Contains(relative) && !(allowCompressedVariants && declared.Contains(compressedBase))) errors.Add($"Undeclared public course file: {entry.CourseId}/{relative}");
            }
            if (manifest.ContentFingerprint != ContentFingerprint.Compute(fingerprintFiles)) errors.Add($"Fingerprint mismatch: {entry.CourseId}");
        }
        if (errors.Count > 0) throw new InvalidOperationException(string.Join(" | ", errors));
    }

    private static async Task<T> ReadReferencedAsync<T>(string root, List<ContentReference> references, List<(string Path, byte[] Bytes)> fingerprintFiles) where T : class
    {
        if (references.Count != 1) throw new InvalidOperationException($"Expected one {typeof(T).Name} reference.");
        var path = references[0].Path;
        var bytes = await File.ReadAllBytesAsync(SafeCourseFile(root, path));
        fingerprintFiles.Add((path, bytes));
        return JsonSerializer.Deserialize<T>(bytes, CourseJson.Options) ?? throw new InvalidOperationException($"Empty referenced file: {path}");
    }

    private static async Task<T> ReadAsync<T>(string path) where T : class =>
        JsonSerializer.Deserialize<T>(await File.ReadAllBytesAsync(path), CourseJson.Options) ?? throw new InvalidOperationException($"Empty JSON: {path}");

    private static string SafeCourseFile(string courseRoot, string relative)
    {
        if (!CoursePathResolver.IsSafeRelativePath(relative)) throw new InvalidOperationException($"Unsafe course path: {relative}");
        var root = Path.GetFullPath(courseRoot);
        var target = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!target.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException($"Course path escaped root: {relative}");
        return target;
    }

    private static void EnsureOutsideSource(string sourceRoot, string target)
    {
        var source = Path.GetFullPath(sourceRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (Path.GetFullPath(target).StartsWith(source, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Output may not be written inside original CourseSources.");
    }

    private static async Task PublishMaterialsAsync(string englishRoot, string coursesRoot)
    {
        EnsureOutsideSource(englishRoot, coursesRoot);
        var source = Directory.EnumerateDirectories(englishRoot, "english-life-office-it", SearchOption.AllDirectories).Single();
        var courseRoot = Path.Combine(coursesRoot, "English", "english-life-office-it");
        var manifestPath = Path.Combine(courseRoot, "manifest.json");
        var manifest = await ReadAsync<CourseManifest>(manifestPath);
        var sourcePdf = Path.Combine(source, "PDF");
        var names = new[] { "StudentBook_Units01-36.pdf", "AnswerKey_Units01-36.pdf" };
        var payloads = names.Select(name => (Name: name, Bytes: File.ReadAllBytes(Path.Combine(sourcePdf, name)))).ToArray();
        if (payloads.Any(x => x.Bytes.Length < 5 || !x.Bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8))) throw new InvalidDataException("Source PDF is invalid.");
        var downloads = Path.Combine(courseRoot, "Downloads");
        Directory.CreateDirectory(downloads);
        foreach (var item in payloads) await File.WriteAllBytesAsync(Path.Combine(downloads, item.Name), item.Bytes);
        var zipName = "EnglishForLifeOfficeIT_CompleteCourse.zip";
        var zipPath = Path.Combine(downloads, zipName);
        using (var stream = File.Create(zipPath))
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
            foreach (var item in payloads)
            {
                var entry = archive.CreateEntry(item.Name, CompressionLevel.Optimal);
                using var output = entry.Open();
                output.Write(item.Bytes);
            }
        using (var archive = ZipFile.OpenRead(zipPath))
            foreach (var item in payloads)
            {
                var entry = archive.GetEntry(item.Name) ?? throw new InvalidDataException("Missing ZIP member.");
                using var input = entry.Open();
                using var buffer = new MemoryStream();
                input.CopyTo(buffer);
                if (!buffer.ToArray().AsSpan().SequenceEqual(item.Bytes)) throw new InvalidDataException("ZIP member differs from source.");
            }
        var references = new List<DownloadReference>
        {
            new() { Id = "student-book-pdf", Title = "Student Book · Units 01–36", Description = "Original student textbook with lessons and exercises.", Category = "student-book", Format = "pdf", Path = $"Downloads/{names[0]}", FileSize = payloads[0].Bytes.Length, SourceLanguage = "en", DisplayOrder = 1 },
            new() { Id = "answer-key-pdf", Title = "Answer Key · Units 01–36", Description = "Original answer key for checking completed exercises.", Category = "answer-key", Format = "pdf", Path = $"Downloads/{names[1]}", FileSize = payloads[1].Bytes.Length, SourceLanguage = "en", DisplayOrder = 2 },
            new() { Id = "complete-course-zip", Title = "Complete course PDF package", Description = "Student Book and Answer Key PDFs.", Category = "full-package", Format = "zip", Path = $"Downloads/{zipName}", FileSize = new FileInfo(zipPath).Length, SourceLanguage = "en", DisplayOrder = 3 }
        };
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest with { Downloads = references }, CourseJson.Options));
        Console.WriteLine($"Published {references.Count} verified course downloads.");
    }

    public static async Task ValidatePublishAsync(string wwwroot, string basePath)
    {
        if (!basePath.StartsWith('/') || !basePath.EndsWith('/') || basePath.Contains("..", StringComparison.Ordinal) || basePath.Contains("//", StringComparison.Ordinal)) throw new InvalidOperationException("Invalid base path.");
        var required = new[] { "index.html", "404.html", ".nojekyll", "manifest.webmanifest", "icon-192.png", "icon-512.png", "service-worker.js", "service-worker-assets.js", "Courses/catalog.json" };
        foreach (var name in required) if (!File.Exists(Path.Combine(wwwroot, name))) throw new InvalidOperationException($"Missing published asset: {name}");
        var index = await File.ReadAllTextAsync(Path.Combine(wwwroot, "index.html"));
        if (!index.Contains($"<base href=\"{basePath}\" />", StringComparison.Ordinal) || !index.Contains("serviceWorker.register", StringComparison.Ordinal) || !index.Contains("deepLink", StringComparison.Ordinal) || index.Contains("{fingerprint}", StringComparison.Ordinal)) throw new InvalidOperationException("Published base path, runtime script, or SPA handling is invalid.");
        var fallback = await File.ReadAllTextAsync(Path.Combine(wwwroot, "404.html"));
        if (!fallback.Contains($"const base = \"{basePath}\"", StringComparison.Ordinal)) throw new InvalidOperationException("404 fallback base path differs.");
        using var webManifest = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(wwwroot, "manifest.webmanifest")));
        if (webManifest.RootElement.GetProperty("display").GetString() != "standalone" || webManifest.RootElement.GetProperty("start_url").GetString() != "./" || webManifest.RootElement.GetProperty("scope").GetString() != "./") throw new InvalidOperationException("PWA manifest is invalid.");
        var worker = await File.ReadAllTextAsync(Path.Combine(wwwroot, "service-worker.js"));
        if (!worker.Contains("service-worker-assets.js", StringComparison.Ordinal) || !worker.Contains("linguaflow-course-", StringComparison.Ordinal)) throw new InvalidOperationException("Published service worker is missing offline support.");
        foreach (var path in Directory.EnumerateFiles(wwwroot, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(wwwroot, path).Replace('\\', '/');
            if (relative.Contains("TRACKING/", StringComparison.OrdinalIgnoreCase) || relative.Contains("CourseSources/", StringComparison.OrdinalIgnoreCase) ||
                relative.EndsWith("PROGRESS.md", StringComparison.OrdinalIgnoreCase) || relative.EndsWith(".env", StringComparison.OrdinalIgnoreCase) ||
                relative.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase) || relative.EndsWith(".pem", StringComparison.OrdinalIgnoreCase) ||
                relative.EndsWith(".key", StringComparison.OrdinalIgnoreCase) || relative.Contains("backup", StringComparison.OrdinalIgnoreCase) && relative.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Private or developer asset in publish output: {relative}");
        }
        await ValidatePublishedAsync(Path.Combine(wwwroot, "Courses"), allowCompressedVariants: true);
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage:\n  convert <CourseSources/English> <wwwroot/Courses> <report.md>\n  rebuild <CourseSources/English> <wwwroot/Courses> <report.md>\n  publish-materials <CourseSources/English> <wwwroot/Courses>\n  validate <wwwroot/Courses>\n  validate-publish <published-wwwroot> </project/>\n  report <CourseSources/English> <report.md>");
        return 2;
    }
}
