using Microsoft.JSInterop;

namespace LinguaFlow.Web.Services;

public sealed record OfflineCourseStatus
{
    public bool Available { get; init; }
    public int CachedUnits { get; init; }
    public int UnitCount { get; init; }
    public long ContentBytes { get; init; }
    public string? ContentVersion { get; init; }
    public string? ContentFingerprint { get; init; }
}

public sealed record OfflineDownloadResult
{
    public string Status { get; init; } = "";
    public string? PreviousVersion { get; init; }
    public string? NewVersion { get; init; }
    public int CachedUnits { get; init; }
    public int UnitCount { get; init; }
    public long ContentBytes { get; init; }
}

public sealed class OfflineCourseService(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? _module;
    private IJSObjectReference? _networkSubscription;
    private DotNetObjectReference<OfflineCourseService>? _reference;
    public bool? OnlineHint { get; private set; }
    public event Action? Changed;
    public event Action<int, int>? Progress;

    public async Task InitializeAsync()
    {
        if (_module is not null) return;
        _module = await js.InvokeAsync<IJSObjectReference>("import", "./js/offline.js");
        _reference = DotNetObjectReference.Create(this);
        OnlineHint = await _module.InvokeAsync<bool>("networkHint");
        _networkSubscription = await _module.InvokeAsync<IJSObjectReference>("observeNetwork", _reference);
        Changed?.Invoke();
    }

    public async Task<OfflineCourseStatus> StatusAsync(string courseId)
    {
        await InitializeAsync();
        return await _module!.InvokeAsync<OfflineCourseStatus>("status", courseId);
    }

    public async Task<OfflineDownloadResult> DownloadAsync(string courseId, string languageDirectory, Uri manifestUrl, string fingerprint, bool allowChanged)
    {
        await InitializeAsync();
        return await _module!.InvokeAsync<OfflineDownloadResult>("download", courseId, languageDirectory, manifestUrl.AbsoluteUri, fingerprint, allowChanged, _reference);
    }

    public async Task RemoveAsync(string courseId)
    {
        await InitializeAsync();
        await _module!.InvokeAsync<OfflineDownloadResult>("remove", courseId);
    }

    [JSInvokable] public void OfflineProgress(int done, int total) => Progress?.Invoke(done, total);
    [JSInvokable] public void NetworkChanged(bool online) { OnlineHint = online; Changed?.Invoke(); }

    public async ValueTask DisposeAsync()
    {
        if (_networkSubscription is not null) { try { await _networkSubscription.InvokeVoidAsync("dispose"); await _networkSubscription.DisposeAsync(); } catch (JSDisconnectedException) { } }
        if (_module is not null) { try { await _module.DisposeAsync(); } catch (JSDisconnectedException) { } }
        _reference?.Dispose();
    }
}
