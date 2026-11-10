using System.Text.Json;
using LinguaFlow.Core.Courses;
using LinguaFlow.Core.Progress;
using LinguaFlow.Web.Services;
using Xunit;

namespace LinguaFlow.Web.Tests;

public sealed class CourseBackupTests
{
    private static CourseBackupService Service => new(null!, null!, new BackupValidator());
    private static CourseManifest Manifest => CourseFormatFixture.Manifest();
    private static CourseCatalogEntry Entry => CourseFormatFixture.Entry();

    [Fact]
    public async Task InvalidJson_IsRejectedBeforeAnyWrite()
    {
        var preview = await Service.PreviewAsync("{broken", Entry, Manifest);
        Assert.NotEmpty(preview.Errors);
        Assert.Equal(0, preview.IncomingCount);
        var missingHeader = await Service.PreviewAsync("{\"courseId\":\"sample\"}", Entry, Manifest);
        Assert.NotEmpty(missingHeader.Errors);
    }

    [Fact]
    public async Task WrongCourseAndVersion_AreRejectedBeforeUnitFetch()
    {
        var backup = CourseBackupService.CreateBackup(Manifest, [], DateTimeOffset.UtcNow);
        var wrongCourse = await Service.PreviewAsync(JsonSerializer.Serialize(backup with { CourseId = "other" }), Entry, Manifest);
        var wrongVersion = await Service.PreviewAsync(JsonSerializer.Serialize(backup with { ContentVersion = "old" }), Entry, Manifest);
        Assert.Contains(wrongCourse.Errors, x => x.Contains("different course"));
        Assert.Contains(wrongVersion.Errors, x => x.Contains("migration"));
    }

    [Fact]
    public void ExportGroupsRecordsAndExcludesOtherCourse()
    {
        LearningRecord Make(string course, LearningRecordKind kind, string id) => new()
        {
            CourseId = course, Kind = kind, Id = id, Data = JsonSerializer.SerializeToElement("text"), UpdatedAt = DateTimeOffset.UtcNow, RecordSchemaVersion = 1
        };
        var backup = CourseBackupService.CreateBackup(Manifest,
            [Make("sample", LearningRecordKind.Note, "note"), Make("other", LearningRecordKind.Note, "private"), Make("sample", LearningRecordKind.WritingDraft, "draft"), Make("sample", LearningRecordKind.Mistake, "mistake")], DateTimeOffset.UtcNow);
        Assert.Single(backup.Notes);
        Assert.Single(backup.Drafts);
        Assert.Single(backup.Mistakes);
        Assert.Equal(3, backup.AllRecords().Count());
        Assert.DoesNotContain("private", JsonSerializer.Serialize(backup));
    }
}
