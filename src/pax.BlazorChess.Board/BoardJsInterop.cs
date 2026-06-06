using Microsoft.JSInterop;

namespace pax.BlazorChess.Board;

public interface IBoardJsInterop
{
    ValueTask DisposeAsync();
    ValueTask<int?> GetSquareIndexFromPoint(double clientX, double clientY, Guid boardGuid);
    ValueTask ReleasePointerCapture(Guid boardGuid, int drawingPointerId);
    ValueTask SetPointerCapture(Guid boardGuid, int drawingPointerId);
    ValueTask UpdateBoardSize(Guid boardGuid);
    ValueTask ScrollToElement(string elementId, string behavior = "auto");
    ValueTask ScrollToBottom(string elementId, string containerId);
}

public class BoardJsInterop(IJSRuntime jsRuntime) : IAsyncDisposable, IBoardJsInterop
{
    private readonly Lazy<Task<IJSObjectReference>> moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/pax.BlazorChess.Board/boardJsInterop.js").AsTask());

    public async ValueTask SetPointerCapture(Guid boardGuid, int drawingPointerId)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("setPointerCapture", boardGuid, drawingPointerId);
    }

    public async ValueTask ReleasePointerCapture(Guid boardGuid, int drawingPointerId)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("releasePointerCapture", boardGuid, drawingPointerId);
    }

    public async ValueTask<int?> GetSquareIndexFromPoint(double clientX, double clientY, Guid boardGuid)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<int?>(
                    "getSquareIndexFromPoint",
                    clientX,
                    clientY,
                    boardGuid);
    }

    public async ValueTask UpdateBoardSize(Guid boardGuid)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("updateBoardSize", boardGuid);
    }

    public async ValueTask ScrollToElement(string elementId, string behavior = "auto")
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("scrollToElement", elementId, behavior);
    }

    public async ValueTask ScrollToBottom(string elementId, string containerId)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("scrollToBottom", elementId, containerId);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (moduleTask.IsValueCreated)
            {
                var module = await moduleTask.Value;
                await module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
