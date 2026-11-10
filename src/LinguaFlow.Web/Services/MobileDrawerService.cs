using Microsoft.JSInterop;

namespace LinguaFlow.Web.Services;

public sealed class MobileDrawerService(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? _module;

    public async Task SetOpenAsync(bool open)
    {
        try
        {
            _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/mobile-drawer.js");
            await _module.InvokeVoidAsync(open ? "open" : "close");
        }
        catch (JSException) { /* Navigation links remain available without focus enhancement. */ }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is null) return;
        try { await _module.InvokeVoidAsync("close", false); await _module.DisposeAsync(); }
        catch (JSDisconnectedException) { }
    }
}
