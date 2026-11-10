using Microsoft.JSInterop;

namespace LinguaFlow.Web.Services;
public sealed class StudyTimerService(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? _module;
    private async Task<IJSObjectReference> ModuleAsync() => _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/study-timer.js");
    public async Task StartAsync() => await (await ModuleAsync()).InvokeVoidAsync("start");
    public async Task<int> StopAsync() => _module is null ? 0 : await _module.InvokeAsync<int>("stop");
    public async ValueTask DisposeAsync() { if (_module is not null) { await _module.InvokeVoidAsync("stop"); await _module.DisposeAsync(); } }
}
