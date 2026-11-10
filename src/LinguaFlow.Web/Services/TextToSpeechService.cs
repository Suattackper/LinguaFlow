using System.Text.Json;
using LinguaFlow.Core.Progress;
using Microsoft.JSInterop;

namespace LinguaFlow.Web.Services;

public sealed record SpeechVoice(string VoiceUri, string Name, string Lang, bool LocalService);

public sealed class TextToSpeechService(IJSRuntime js, LearningRepository repository) : IAsyncDisposable
{
    private IJSObjectReference? _module;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly SemaphoreSlim _initGate = new(1, 1);
    private readonly Dictionary<string, string> _preferred = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<SpeechVoice> Voices { get; private set; } = [];
    public bool Available { get; private set; }
    public double Rate { get; private set; } = 1;
    public string? Error { get; private set; }
    private bool _initialized;
    private async Task<IJSObjectReference> ModuleAsync()
    {
        if (_module is not null) return _module;
        await _gate.WaitAsync();
        try { return _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/speech.js"); }
        finally { _gate.Release(); }
    }
    public async Task InitializeAsync()
    {
        if (_initialized) return;
        await _initGate.WaitAsync();
        try
        {
            if (_initialized) return;
            var module = await ModuleAsync();
            Available = await module.InvokeAsync<bool>("supported");
            Voices = Available ? await module.InvokeAsync<List<SpeechVoice>>("voices") : [];
            try
            {
                foreach (var record in await repository.UserPreferences.GetByCourseAsync("global"))
                {
                    if (record.Id == "tts-rate" && record.Data.ValueKind == JsonValueKind.Number) Rate = Math.Clamp(record.Data.GetDouble(), 0.6, 1.4);
                    else if (record.Id.StartsWith("tts-voice-", StringComparison.Ordinal) && record.Data.ValueKind == JsonValueKind.String) _preferred[record.Id[10..]] = record.Data.GetString() ?? "";
                }
            }
            catch (LearningStorageException) { }
            _initialized = true;
            Error = Available ? null : "SpeechUnavailable";
        }
        catch (JSException) { Available = false; Error = "SpeechUnavailable"; _initialized = true; }
        finally { _initGate.Release(); }
    }
    public SpeechVoice? SelectVoice(string language)
    {
        _preferred.TryGetValue(language.ToLowerInvariant(), out var uri);
        return ChooseVoice(Voices, language, uri);
    }
    public async Task RefreshVoicesAsync()
    {
        await InitializeAsync();
        if (!Available) return;
        try { Voices = await (await ModuleAsync()).InvokeAsync<List<SpeechVoice>>("voices", 2500); Error = null; }
        catch (JSException) { Error = "SpeechVoiceMissing"; }
    }
    public static SpeechVoice? ChooseVoice(IEnumerable<SpeechVoice> voices, string language, string? preferredUri)
    {
        var candidates = voices.Where(v => v.Lang.Equals(language, StringComparison.OrdinalIgnoreCase)).ToList();
        if (candidates.Count == 0) candidates = voices.Where(v => v.Lang.StartsWith(language[..2], StringComparison.OrdinalIgnoreCase)).ToList();
        return preferredUri is not null ? candidates.FirstOrDefault(v => v.VoiceUri == preferredUri) ?? candidates.FirstOrDefault() : candidates.FirstOrDefault();
    }
    public async Task SetVoiceAsync(string language, string voiceUri)
    {
        if (!new[] { "en-US", "en-GB", "ja-JP", "zh-CN" }.Contains(language) || !Voices.Any(v => v.VoiceUri == voiceUri && v.Lang.StartsWith(language[..2], StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("Voice does not match the requested language.");
        _preferred[language.ToLowerInvariant()] = voiceUri;
        await PersistAsync("tts-voice-" + language.ToLowerInvariant(), voiceUri);
    }
    public async Task SetRateAsync(double rate)
    {
        Rate = Math.Clamp(rate, 0.6, 1.4);
        await PersistAsync("tts-rate", Rate);
    }
    private async Task PersistAsync<T>(string id, T value)
    {
        try { await repository.UserPreferences.SaveAsync(new LearningRecord { CourseId = "global", Kind = LearningRecordKind.UserPreference, Id = id, Data = JsonSerializer.SerializeToElement(value), UpdatedAt = DateTimeOffset.UtcNow, RecordSchemaVersion = 1 }); }
        catch (LearningStorageException) { Error = "SpeechPreferenceSaveFailed"; }
    }
    public async Task SpeakAsync(string text, string language)
    {
        await InitializeAsync();
        if (!Available) { Error = "SpeechUnavailable"; return; }
        var voice = SelectVoice(language);
        if (voice is null) { Error = "SpeechVoiceMissing"; return; }
        try { Error = null; await (await ModuleAsync()).InvokeVoidAsync("speak", text, language, voice.VoiceUri, Rate); }
        catch (JSException ex) { Error = ex.Message.Contains("stopped", StringComparison.OrdinalIgnoreCase) ? null : "SpeechPlaybackFailed"; }
    }
    public async Task StopAsync()
    {
        if (_module is not null) try { await _module.InvokeVoidAsync("stop"); } catch (JSException) { }
    }
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        if (_module is not null) await _module.DisposeAsync();
        _gate.Dispose();
        _initGate.Dispose();
    }
}
