using LinguaFlow.Web.Services;
using Xunit;

namespace LinguaFlow.Web.Tests;

public sealed class ReaderWorkspaceTests
{
    [Fact]
    public async Task PositionDraftNoteAndBookmark_AreScopedToCourseAndUnit()
    {
        IReaderWorkspace workspace = new SessionReaderWorkspace();
        await workspace.SetPositionAsync("english", "unit-01", "grammar");
        await workspace.SetDraftAsync("english", "unit-01", "exercise-01", "My draft");
        await workspace.SetNoteAsync("english", "unit-01", "grammar", "Remember this");
        await workspace.SetBookmarkAsync("english", "unit-01", "grammar", true);

        Assert.Equal("unit-01", workspace.GetLastUnit("english"));
        Assert.Equal("grammar", workspace.GetLastSection("english", "unit-01"));
        Assert.Equal("My draft", workspace.GetDraft("english", "unit-01", "exercise-01"));
        Assert.Equal("Remember this", workspace.GetNote("english", "unit-01", "grammar"));
        Assert.True(workspace.IsBookmarked("english", "unit-01", "grammar"));
        Assert.Equal("", workspace.GetDraft("other", "unit-01", "exercise-01"));
        Assert.False(workspace.IsBookmarked("english", "unit-02", "grammar"));
        await workspace.SetBookmarkAsync("english", "unit-01", "grammar", false);
        Assert.False(workspace.IsBookmarked("english", "unit-01", "grammar"));
    }
}
