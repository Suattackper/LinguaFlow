using System.Text.Json;
using Bunit;
using LinguaFlow.Web.Components;
using Xunit;

namespace LinguaFlow.Web.Tests;

public sealed class MarkdownCoverageTests : BunitContext
{
    [Fact]
    public void AuthoredMarkdownHeadingsStayBelowLessonSectionHeading()
    {
        var rendered = Render<SafeMarkdown>(p => p.Add(x => x.Text, "# Tiêu đề **đậm**\n\n- Một mục\n- Mục hai"));
        Assert.Empty(rendered.FindAll("h1, h2"));
        Assert.Equal("Tiêu đề đậm", rendered.Find("h3").TextContent);
        Assert.Equal(2, rendered.FindAll("li").Count);
    }

    [Fact]
    public void EveryPublishedUnitSectionHasAReadableSafeSample()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "LinguaFlow.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        var course = Path.Combine(root.FullName, "src", "LinguaFlow.Web", "wwwroot", "Courses", "English", "english-life-office-it");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(course, "manifest.json")));
        var references = manifest.RootElement.GetProperty("units").EnumerateArray().ToArray();
        Assert.Equal(36, references.Length);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var reference in references)
        {
            var id = reference.GetProperty("unitId").GetString()!;
            Assert.True(seen.Add(id));
            using var unit = JsonDocument.Parse(File.ReadAllText(Path.Combine(course, reference.GetProperty("path").GetString()!.Replace('/', Path.DirectorySeparatorChar))));
            Assert.Equal(id, unit.RootElement.GetProperty("unitId").GetString());
            foreach (var section in unit.RootElement.GetProperty("sections").EnumerateArray())
            {
                var blocks = section.GetProperty("blocks").EnumerateArray().ToArray();
                var renderedInSection = 0;
                foreach (var block in blocks)
                {
                    var text = new[] { "text", "prompt", "instructions" }.Select(name => block.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
                    if (text is null) continue;
                    var rendered = Render<SafeMarkdown>(p => p.Add(x => x.Text, text));
                    renderedInSection++;
                    Assert.NotEmpty(rendered.Find(".markdown-content").TextContent.Trim());
                    Assert.Empty(rendered.FindAll("script, iframe, object"));
                    Assert.All(rendered.FindAll("a"), link => Assert.DoesNotContain("javascript:", link.GetAttribute("href") ?? "", StringComparison.OrdinalIgnoreCase));
                }
                if (renderedInSection == 0)
                {
                    var rendered = Render<SafeMarkdown>(p => p.Add(x => x.Text, section.GetProperty("title").GetString()));
                    Assert.NotEmpty(rendered.Find(".markdown-content").TextContent.Trim());
                }
            }
        }
        Assert.Equal(36, seen.Count);
    }
}
