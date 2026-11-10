using LinguaFlow.Core.Courses;

namespace LinguaFlow.Core.Tests;

public sealed class DownloadManifestTests
{
    [Fact]
    public void DownloadPathRejectsTraversalAndKeepsProjectBase()
    {
        var resolver = new CoursePathResolver();
        var manifest = new Uri("https://example.org/project/Courses/English/course/manifest.json");
        Assert.Equal("https://example.org/project/Courses/English/course/Downloads/book.pdf", resolver.ResolveContent(manifest, "Downloads/book.pdf").AbsoluteUri);
        Assert.Throws<CourseContentException>(() => resolver.ResolveContent(manifest, "Downloads/../private.json"));
    }
}
