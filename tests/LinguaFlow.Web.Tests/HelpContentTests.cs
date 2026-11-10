using LinguaFlow.Web.Pages;

namespace LinguaFlow.Web.Tests;

public sealed class HelpContentTests
{
    [Fact]
    public void EveryGuideTopicHasVietnameseEnglishStepsAndWorkingInternalRoutes()
    {
        Assert.Equal(9, HelpContent.Articles.Length);
        var routes = new HashSet<string>(StringComparer.Ordinal) { "settings", "languages/en", "courses/english-life-office-it/roadmap", "courses/english-life-office-it/vocabulary", "courses/english-life-office-it/review", "courses/english-life-office-it/practice", "courses/english-life-office-it/practice/import", "courses/english-life-office-it/backup", "courses/english-life-office-it/materials", "courses/english-life-office-it/offline", "courses/english-life-office-it/goals" };
        foreach (var article in HelpContent.Articles)
        {
            Assert.NotEmpty(article.Vi); Assert.NotEmpty(article.En);
            Assert.All(article.Vi.Concat(article.En).Where(x => x.Href is not null), step => Assert.Contains(step.Href!, routes));
        }
        Assert.Contains("phát âm", HelpContent.Articles.Single(x => x.Id == "chatgpt").Vi.Last().Text);
    }
}
