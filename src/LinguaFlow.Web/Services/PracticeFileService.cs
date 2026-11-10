using Microsoft.JSInterop;

namespace LinguaFlow.Web.Services;

public sealed class PracticeFileService(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? _module;
    private Task<IJSObjectReference> ModuleAsync() => _module is not null ? Task.FromResult(_module) : ImportAsync();
    private async Task<IJSObjectReference> ImportAsync() => _module = await js.InvokeAsync<IJSObjectReference>("import", "./js/practice-files.js");
    public async Task DownloadAsync(string filename, string markdown) => await (await ModuleAsync()).InvokeVoidAsync("downloadMarkdown", filename, markdown);
    public async Task CopyAsync(string markdown) => await (await ModuleAsync()).InvokeVoidAsync("copyInstructions", markdown);
    public async Task<bool> OpenChatAsync() => await (await ModuleAsync()).InvokeAsync<bool>("openChat");
    public async ValueTask DisposeAsync() { if (_module is not null) await _module.DisposeAsync(); }
}
