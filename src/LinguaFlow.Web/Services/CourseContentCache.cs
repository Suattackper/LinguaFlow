namespace LinguaFlow.Web.Services;

public sealed class CourseContentCache
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, object> _items = new(StringComparer.Ordinal);

    public async Task<T> GetOrAddAsync<T>(Uri uri, Func<CancellationToken, Task<T>> load, CancellationToken cancellationToken) where T : class
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_items.TryGetValue(uri.AbsoluteUri, out var existing)) return (T)existing;
            var item = await load(cancellationToken);
            _items[uri.AbsoluteUri] = item;
            return item;
        }
        finally { _gate.Release(); }
    }
}
