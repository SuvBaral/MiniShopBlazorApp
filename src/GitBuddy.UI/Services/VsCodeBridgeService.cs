using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.JSInterop;

namespace GitBuddy.UI.Services;

public class VsCodeBridgeService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Dictionary<string, TaskCompletionSource<string>> _pending = new();
    private DotNetObjectReference<VsCodeBridgeService>? _dotNetRef;

    public event Action? OnRepoChanged;
    public event Action? OnFocusNLBar;

    public VsCodeBridgeService(IJSRuntime js) => _js = js;

    public async Task InitializeAsync()
    {
        _dotNetRef = DotNetObjectReference.Create(this);
        await _js.InvokeVoidAsync("bridge.init", _dotNetRef);
    }

    // [DynamicallyAccessedMembers] on T tells the IL trimmer to preserve public constructors and
    // properties of every concrete type used with this method (GitCommandResult, NLTranslation, etc.)
    // so that System.Text.Json reflection-based deserialization works in TrimMode=full Blazor WASM.
    public async Task<T?> SendAsync<[DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors |
        DynamicallyAccessedMemberTypes.PublicProperties |
        DynamicallyAccessedMemberTypes.PublicFields)] T>(
        string command, JsonObject? payload = null, TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(5);
        var id = Guid.NewGuid().ToString("N");
        var tcs = new TaskCompletionSource<string>();
        _pending[id] = tcs;

        var msg = new JsonObject
        {
            ["requestId"] = id,
            ["command"] = command,
            ["payload"] = payload
        };
        await _js.InvokeVoidAsync("bridge.send", msg);

        var timeoutTask = Task.Delay(effectiveTimeout);
        var completed = await Task.WhenAny(tcs.Task, timeoutTask);

        if (completed == timeoutTask)
        {
            _pending.Remove(id);
            tcs.TrySetCanceled();  // Release the TCS immediately, don't wait for GC
            throw new TimeoutException($"Command '{command}' timed out after {effectiveTimeout.TotalSeconds}s");
        }
        _pending.Remove(id);

        var json = await tcs.Task;
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        });
    }

    [JSInvokable]
    public void OnResponse(string requestId, string json)
    {
        if (_pending.TryGetValue(requestId, out var tcs)) tcs.SetResult(json);
    }

    [JSInvokable]
    public void OnRepoChange() => OnRepoChanged?.Invoke();

    [JSInvokable]
    public void OnFocusNLBarRequest() => OnFocusNLBar?.Invoke();

    public async ValueTask DisposeAsync() => _dotNetRef?.Dispose();
}
