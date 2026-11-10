using System.Globalization;
using Microsoft.JSInterop;
using System.Text.Json;
using LinguaFlow.Core.Progress;

namespace LinguaFlow.Web.Services;

public sealed class UiPreferences(IJSRuntime js, LearningRepository learning)
{
    public string Language { get; private set; } = "vi-VN";
    public string Theme { get; private set; } = "system";
    public bool SystemDark { get; private set; }
    public bool ReducedMotion { get; private set; }
    public bool LargeText { get; private set; }
    public event Action? Changed;

    public async Task InitializeAsync()
    {
        try
        {
            Language = await js.InvokeAsync<string?>("linguaFlow.get", "language") == "en-US" ? "en-US" : "vi-VN";
            var storedTheme = await js.InvokeAsync<string?>("linguaFlow.get", "theme");
            Theme = storedTheme is "light" or "dark" ? storedTheme : "system";
            ReducedMotion = await js.InvokeAsync<string?>("linguaFlow.get", "reducedMotion") == "true";
            LargeText = await js.InvokeAsync<string?>("linguaFlow.get", "largeText") == "true";
            await ApplyAsync();
            await MirrorPreferencesAsync();
        }
        catch (JSException) { }
    }

    public async Task SetLanguageAsync(string language)
    {
        Language = language == "en-US" ? "en-US" : "vi-VN";
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo(Language);
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo(Language);
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(Language);
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(Language);
        await SaveAsync("language", Language);
        Changed?.Invoke();
    }

    public async Task SetThemeAsync(string theme)
    {
        Theme = theme is "light" or "dark" ? theme : "system";
        await SaveAsync("theme", Theme);
        await ApplyAsync();
        Changed?.Invoke();
    }

    public async Task SetReducedMotionAsync(bool value)
    {
        ReducedMotion = value;
        await SaveAsync("reducedMotion", value ? "true" : "false");
        await ApplyAsync();
        Changed?.Invoke();
    }

    public async Task SetLargeTextAsync(bool value)
    {
        LargeText = value;
        await SaveAsync("largeText", value ? "true" : "false");
        await ApplyAsync();
        Changed?.Invoke();
    }

    public async Task RestoreDefaultsAsync()
    {
        await SetLanguageAsync("vi-VN");
        await SetThemeAsync("system");
        await SetReducedMotionAsync(false);
        await SetLargeTextAsync(false);
    }

    private async Task SaveAsync(string key, string value)
    {
        try { await js.InvokeVoidAsync("linguaFlow.set", key, value); }
        catch (JSException) { }
        await MirrorAsync(key, value);
    }

    private async Task MirrorPreferencesAsync()
    {
        await MirrorAsync("language", Language); await MirrorAsync("theme", Theme);
        await MirrorAsync("reducedMotion", ReducedMotion ? "true" : "false");
        await MirrorAsync("largeText", LargeText ? "true" : "false");
    }
    private async Task MirrorAsync(string key, string value)
    {
        var id = key switch { "reducedMotion" => "reduced-motion", "largeText" => "large-text", _ => key };
        try { await learning.UserPreferences.SaveAsync(new LearningRecord { CourseId = "global", Kind = LearningRecordKind.UserPreference, Id = id, Data = JsonSerializer.SerializeToElement(value), UpdatedAt = DateTimeOffset.UtcNow, RecordSchemaVersion = 1 }); }
        catch (LearningStorageException) { /* UI preferences remain in their bootstrap localStorage copy. */ }
    }

    private async Task ApplyAsync()
    {
        try { SystemDark = await js.InvokeAsync<bool>("linguaFlow.apply", Theme, ReducedMotion, LargeText, Language); }
        catch (JSException) { }
    }
}
