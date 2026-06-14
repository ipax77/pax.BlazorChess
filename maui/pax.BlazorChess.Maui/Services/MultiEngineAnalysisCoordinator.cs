using System.Globalization;
using System.Text;
using pax.chess;
using pax.chess.Analyze;
using pax.chess.Extensions;
using pax.uciChessEngine;
using pax.uciChessEngine.EngineServices;

namespace pax.BlazorChess.Maui.Services;

public sealed class MultiEngineAnalysisCoordinator : IAsyncDisposable
{
    private static readonly TimeSpan RestartDebounce = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan UiThrottle = TimeSpan.FromMilliseconds(125);

    private readonly object _gate = new();
    private readonly Dictionary<Guid, EngineAnalysisSnapshot> _snapshots = [];
    private CancellationTokenSource? _analysisCts;
    private Task? _runTask;
    private DateTimeOffset _lastUiUpdate = DateTimeOffset.MinValue;
    private bool _renderQueued;
    private int _runId;

    public event Func<Task>? Changed;

    public bool IsActive { get; private set; }

    public IReadOnlyList<EngineAnalysisSnapshot> Snapshots
    {
        get
        {
            lock (_gate)
                return _snapshots.Values.ToList();
        }
    }

    public EngineComparisonSnapshot Comparison
        => EngineComparisonBuilder.Build(Snapshots);

    public async Task StartAsync(
        AnalysisBoard analysisBoard,
        IReadOnlyList<EngineRunOptions> engines,
        int maxParallelEngines = 2)
    {
        ArgumentNullException.ThrowIfNull(analysisBoard);
        ArgumentNullException.ThrowIfNull(engines);

        StopCurrent(markPaused: false);

        var cts = new CancellationTokenSource();
        var runId = Interlocked.Increment(ref _runId);
        _analysisCts = cts;
        IsActive = true;

        var baseNode = analysisBoard.CurrentNode;
        var basePosition = analysisBoard.CurrentPosition;
        var engineMoves = BuildEngineMoveString(baseNode);
        var sideToMove = basePosition.SideToMove;
        var parallelLimit = Math.Max(1, maxParallelEngines);

        var engineSnapshot = engines as EngineRunOptions[] ?? engines.ToArray();

        InitializeSnapshots(engineSnapshot, runId);
        await RequestRenderAsync(force: true);

        _runTask = RunAllEnginesAsync(
            engineSnapshot,
            baseNode,
            basePosition,
            engineMoves,
            sideToMove,
            parallelLimit,
            runId,
            cts.Token);
    }

    public async Task RestartAsync(
        AnalysisBoard analysisBoard,
        IReadOnlyList<EngineRunOptions> engines,
        int maxParallelEngines = 2)
    {
        if (!IsActive)
            return;

        await StartAsync(analysisBoard, engines, maxParallelEngines);
    }

    public async Task PauseAsync()
    {
        StopCurrent(markPaused: true);
        await RequestRenderAsync(force: true);
    }

    public async Task StopAsync()
    {
        StopCurrent(markPaused: true);
        await RequestRenderAsync(force: true);
    }

    public void Clear(IReadOnlyList<EngineRunOptions> engines)
    {
        StopCurrent(markPaused: false);
        lock (_gate)
        {
            _snapshots.Clear();
            foreach (var engine in engines)
            {
                _snapshots[engine.Id] = EngineAnalysisSnapshot.FromOptions(
                    engine,
                    engine.IsEnabled ? EngineAnalysisStatus.Paused : EngineAnalysisStatus.Disabled,
                    engine.IsEnabled ? "Paused" : "Disabled");
            }
        }

        _ = RequestRenderAsync(force: true);
    }

    private async Task RunAllEnginesAsync(
        IReadOnlyList<EngineRunOptions> engines,
        MoveNode baseNode,
        BoardPosition basePosition,
        string engineMoves,
        PieceColor sideToMove,
        int parallelLimit,
        int runId,
        CancellationToken token)
    {
        try
        {
            await Task.Delay(RestartDebounce, token);
            using var engineSlots = new SemaphoreSlim(parallelLimit, parallelLimit);
            List<Task> tasks = new(engines.Count);
            foreach (var engine in engines)
            {
                if (!engine.IsEnabled || string.IsNullOrWhiteSpace(engine.BinaryPath))
                    continue;

                tasks.Add(RunOneEngineWhenSlotAvailableAsync(
                    engine,
                    baseNode,
                    basePosition,
                    engineMoves,
                    sideToMove,
                    engineSlots,
                    runId,
                    token));
            }

            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task RunOneEngineWhenSlotAvailableAsync(
        EngineRunOptions engine,
        MoveNode baseNode,
        BoardPosition basePosition,
        string engineMoves,
        PieceColor sideToMove,
        SemaphoreSlim engineSlots,
        int runId,
        CancellationToken token)
    {
        try
        {
            await engineSlots.WaitAsync(token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            await RunOneEngineAsync(engine, baseNode, basePosition, engineMoves, sideToMove, runId, token);
        }
        finally
        {
            engineSlots.Release();
        }
    }

    private async Task RunOneEngineAsync(
        EngineRunOptions engine,
        MoveNode baseNode,
        BoardPosition basePosition,
        string engineMoves,
        PieceColor sideToMove,
        int runId,
        CancellationToken token)
    {
        try
        {
            SetSnapshot(runId, EngineAnalysisSnapshot.FromOptions(engine, EngineAnalysisStatus.Starting, "Starting"));
            await RequestRenderAsync(force: true);

            var provider = EngineService.GetEngineSessionProvider(engine);
            await using var lease = await provider.AcquireAsync(token);

            await lease.Session.UseAsync<Task>(async uciEngine =>
            {
                await foreach (var evals in EngineService.GetContinualEvaluation(engineMoves, sideToMove, uciEngine, token))
                {
                    var lines = BuildLines(evals, baseNode, basePosition);
                    var statusText = lines.Count > 0 ? $"Depth {lines[0].Depth}" : "Searching";
                    SetSnapshot(
                        runId,
                        EngineAnalysisSnapshot.FromOptions(
                            engine,
                            EngineAnalysisStatus.Running,
                            statusText,
                            lines: lines));
                    await RequestRenderAsync(force: false);
                }

                return Task.CompletedTask;
            }, token);

            var current = GetSnapshot(engine.Id);
            if (current is not null)
            {
                SetSnapshot(
                    runId,
                    current with
                    {
                        Status = EngineAnalysisStatus.Ready,
                        StatusText = current.Lines.Count > 0 ? current.StatusText : "Ready"
                    });
                await RequestRenderAsync(force: true);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            SetSnapshot(
                runId,
                EngineAnalysisSnapshot.FromOptions(
                    engine,
                    EngineAnalysisStatus.Error,
                    "Error",
                    ex.Message));
            await RequestRenderAsync(force: true);
        }
    }

    private void InitializeSnapshots(IReadOnlyList<EngineRunOptions> engines, int runId)
    {
        lock (_gate)
        {
            if (runId != _runId)
                return;

            _snapshots.Clear();
            foreach (var engine in engines)
            {
                var status = GetInitialStatus(engine, out var statusText, out var error);
                _snapshots[engine.Id] = EngineAnalysisSnapshot.FromOptions(engine, status, statusText, error);
            }
        }
    }

    private void SetSnapshot(int runId, EngineAnalysisSnapshot snapshot)
    {
        lock (_gate)
        {
            if (runId != _runId)
                return;

            _snapshots[snapshot.EngineId] = snapshot;
        }
    }

    private EngineAnalysisSnapshot? GetSnapshot(Guid engineId)
    {
        lock (_gate)
            return _snapshots.GetValueOrDefault(engineId);
    }

    private void StopCurrent(bool markPaused)
    {
        var cts = Interlocked.Exchange(ref _analysisCts, null);
        if (cts is not null)
        {
            cts.Cancel();
            cts.Dispose();
        }

        Interlocked.Increment(ref _runId);
        IsActive = false;

        if (!markPaused)
            return;

        lock (_gate)
        {
            foreach (var (id, snapshot) in _snapshots.ToList())
            {
                if (snapshot.Status is EngineAnalysisStatus.Disabled or EngineAnalysisStatus.Error)
                    continue;

                _snapshots[id] = snapshot with
                {
                    Status = EngineAnalysisStatus.Paused,
                    StatusText = "Paused"
                };
            }
        }
    }

    private async Task RequestRenderAsync(bool force)
    {
        var now = DateTimeOffset.UtcNow;
        if (!force && now - _lastUiUpdate < UiThrottle)
            return;

        if (_renderQueued)
            return;

        _renderQueued = true;
        try
        {
            var handler = Changed;
            if (handler is not null)
                await handler.Invoke();
        }
        finally
        {
            _lastUiUpdate = DateTimeOffset.UtcNow;
            _renderQueued = false;
        }
    }

    private static EngineAnalysisStatus GetInitialStatus(
        EngineRunOptions engine,
        out string statusText,
        out string error)
    {
        error = string.Empty;
        if (!engine.IsEnabled)
        {
            statusText = "Disabled";
            return EngineAnalysisStatus.Disabled;
        }

        if (string.IsNullOrWhiteSpace(engine.BinaryPath))
        {
            statusText = "Error";
            error = "Set an engine binary path before starting analysis.";
            return EngineAnalysisStatus.Error;
        }

        statusText = "Queued";
        return EngineAnalysisStatus.Queued;
    }

    internal static List<EnginePvLineSnapshot> BuildLines(
        List<Eval> evals,
        MoveNode baseNode,
        BoardPosition basePosition)
    {
        List<EnginePvLineSnapshot> lines = [];
        foreach (var eval in evals.OrderBy(e => e.PvInfo.MultiPv))
        {
            var moves = BuildPvMoves(eval.PvInfo.Moves, basePosition);
            if (moves.Count == 0)
                continue;

            lines.Add(new EnginePvLineSnapshot(
                eval.PvInfo.MultiPv,
                FormatEval(eval),
                eval.Score,
                eval.Mate,
                eval.Depth,
                baseNode,
                moves.Select(static move => move.Uci).ToArray(),
                moves));
        }

        return lines;
    }

    private static List<EnginePvMoveSnapshot> BuildPvMoves(
        IReadOnlyList<string> engineMoves,
        BoardPosition basePosition)
    {
        List<EnginePvMoveSnapshot> moves = [];
        var position = basePosition;

        foreach (var engineMove in engineMoves)
        {
            if (string.IsNullOrWhiteSpace(engineMove)
                || engineMove.Equals("(none)", StringComparison.OrdinalIgnoreCase)
                || !TryCreateMoveSnapshot(engineMove, position, out var snapshot, out var nextPosition))
            {
                break;
            }

            moves.Add(snapshot);
            position = nextPosition;
        }

        return moves;
    }

    private static bool TryCreateMoveSnapshot(
        string engineMove,
        BoardPosition position,
        out EnginePvMoveSnapshot snapshot,
        out BoardPosition nextPosition)
    {
        snapshot = default!;
        nextPosition = position;

        try
        {
            var move = Uci.CreateMove(engineMove, position);
            if (move is null)
                return false;

            var san = PgnSerializer.ToSan(move, position);
            nextPosition = position.MakeMove(move);
            snapshot = new EnginePvMoveSnapshot(engineMove, san, move);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static string BuildEngineMoveString(MoveNode node)
    {
        Stack<Move> moves = [];
        var current = node;
        while (current.Move is not null)
        {
            moves.Push(current.Move);
            current = current.Parent!;
        }

        if (moves.Count == 0)
            return string.Empty;

        StringBuilder builder = new();
        while (moves.Count > 0)
        {
            if (builder.Length > 0)
                builder.Append(' ');

            builder.Append(Uci.GetUci(moves.Pop()));
        }

        return builder.ToString();
    }

    private static string FormatEval(Eval eval)
    {
        if (eval.Mate != 0)
            return $"M{eval.Mate}";

        return (eval.Score / 100.0).ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture);
    }

    public async ValueTask DisposeAsync()
    {
        StopCurrent(markPaused: false);
        if (_runTask is not null)
        {
            try
            {
                await _runTask;
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
