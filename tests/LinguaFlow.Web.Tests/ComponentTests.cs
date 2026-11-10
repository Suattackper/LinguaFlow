using Bunit;
using LinguaFlow.Web.Components;
using LinguaFlow.Core.Courses;
using LinguaFlow.Core.Assessment;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace LinguaFlow.Web.Tests;

public sealed class ComponentTests : BunitContext
{
    [Fact]
    public void AppSelect_ShowsOneValueAndReportsChosenOption()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        string? chosen = null;
        var cut = Render<AppSelect>(parameters => parameters
            .Add(p => p.Label, "Theme")
            .Add(p => p.Value, "light")
            .Add(p => p.Options, [new SelectItem("light", "Light"), new SelectItem("dark", "Dark")])
            .Add(p => p.ValueChanged, value => chosen = value));
        Assert.Equal("Light", cut.Find(".app-select-trigger .app-select-value").TextContent);
        Assert.Empty(cut.FindAll("select"));
        cut.Find(".app-select-trigger").Click();
        Assert.Equal(2, cut.FindAll(".app-select-option").Count);
        cut.FindAll(".app-select-option")[1].Click();
        Assert.Equal("dark", chosen);
        Assert.Empty(cut.FindAll(".app-select-menu"));
    }

    [Fact]
    public void PageHeading_RendersSemanticHeadingAndDescription()
    {
        var cut = Render<PageHeading>(parameters => parameters
            .Add(p => p.Eyebrow, "Guide")
            .Add(p => p.Title, "How it works")
            .Add(p => p.Description, "Choose a language"));
        Assert.Equal("How it works", cut.Find("h1").TextContent);
        Assert.Equal("Choose a language", cut.Find("p").TextContent);
    }

    [Theory]
    [InlineData("error", "alert")]
    [InlineData("empty", "status")]
    [InlineData("loading", "status")]
    public void StateMessage_UsesAppropriateLiveRole(string kind, string role)
    {
        var cut = Render<StateMessage>(parameters => parameters
            .Add(p => p.Kind, kind)
            .Add(p => p.Title, "State"));
        Assert.Equal(role, cut.Find(".state-message").GetAttribute("role"));
        Assert.Equal("State", cut.Find("h2").TextContent);
    }

    [Fact]
    public void QuestionBlock_ShowsAuthoredOptionsWithoutAnswerOrGrade()
    {
        Services.AddLocalization();
        Services.AddSingleton(new LinguaFlow.Web.Services.LearningRepository(JSInterop.JSRuntime));
        var question = new QuestionBlock { BlockId = "q1", ExerciseId = "e1", Prompt = "Choose the correct form", Options = ["A. works", "B. working"] };
        var cut = Render<ContentBlockView>(p => p.Add(x => x.Block, question));
        Assert.Equal(2, cut.FindAll("input[type=radio]").Count);
        Assert.Contains("Choose the correct form", cut.Markup);
        Assert.DoesNotContain("correct", cut.Markup.ToLowerInvariant().Replace("choose the correct form", ""));
    }

    [Fact]
    public void ExplanationBlock_UsesCollapsibleSafeText()
    {
        Services.AddLocalization();
        Services.AddSingleton(new LinguaFlow.Web.Services.LearningRepository(JSInterop.JSRuntime));
        var block = new ExplanationBlock { BlockId = "ex1", Text = "<script>alert('x')</script>" };
        var cut = Render<ContentBlockView>(p => p.Add(x => x.Block, block));
        Assert.Single(cut.FindAll("details"));
        Assert.Empty(cut.FindAll("script"));
        Assert.Contains("&lt;script&gt;", cut.Markup);
    }

    [Fact]
    public void ExerciseInput_RendersKeyboardAccessibleAuthoredChoices()
    {
        Services.AddLocalization();
        var exercise = new ExerciseDefinition { CourseId = "sample", UnitId = "unit-01", ExerciseId = "unit-01-c2-01", Prompt = "Pick one", Skill = "grammar", Kind = ExerciseKind.SingleChoice, Options = ["A. first", "B. second"] };
        var cut = Render<ExerciseInput>(p => p.Add(x => x.Definition, exercise));
        Assert.Equal(2, cut.FindAll("input[type=radio]").Count);
        Assert.Equal("Pick one", cut.Find("legend").TextContent);
        Assert.Equal("unit-01-c2-01", cut.Find("input").GetAttribute("name"));
    }

    [Fact]
    public void FeedbackView_OnlyShowsAuthoredAnswerWhenProvided()
    {
        Services.AddLocalization();
        var feedback = new ExerciseFeedback { Status = GradingStatus.NeedsReview, EarnedScore = 0, MaximumScore = 1, Message = "FeedbackManual" };
        var cut = Render<ExerciseFeedbackView>(p => p.Add(x => x.Feedback, feedback));
        Assert.Empty(cut.FindAll(".exercise-feedback p strong"));
        var withAnswer = Render<ExerciseFeedbackView>(p => p.Add(x => x.Feedback, feedback with { CorrectAnswer = "B", Explanation = "Original explanation" }));
        Assert.Contains("Original explanation", withAnswer.Markup);
    }

    [Fact]
    public void UnitAssessment_GroupsRepeatedSkillAndDisplaysAuthoredRulesAsSafeText()
    {
        Services.AddLocalization();
        var assessment = new UnitAssessment("unit-01", SkillStatus.NotStarted,
            [new("grammar", SkillStatus.NotStarted, 0, 0, "Grammar: **__/23**"),
             new("grammar", SkillStatus.NotStarted, 0, 0, "<script>bad</script> second rule"),
             new("speaking", SkillStatus.NotStarted, 0, 0, "")],
            ["Complete **grammar**"]);
        var cut = Render<UnitAssessmentSummary>(p => p.Add(x => x.Assessment, assessment));
        Assert.Equal(2, cut.FindAll(".skill-status-card").Count);
        Assert.Contains("2", cut.Find(".unit-assessment-summary").TextContent);
        Assert.DoesNotContain("**", cut.Markup);
        Assert.Empty(cut.FindAll("script"));
        Assert.Contains("&lt;script&gt;", cut.Markup);
    }
}
