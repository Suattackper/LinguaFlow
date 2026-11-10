using Microsoft.JSInterop;

namespace LinguaFlow.Web.Services;

public sealed class AnimationService(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? _module;

    public async Task InitializeAsync()
    {
        try
        {
            _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/motion.js");
            await _module.InvokeVoidAsync("initialize", ".app-shell");
        }
        catch (JSException) { /* Content is visible without motion. */ }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is null) return;
        try { await _module.InvokeVoidAsync("dispose"); await _module.DisposeAsync(); }
        catch (JSDisconnectedException) { }
    }
}
